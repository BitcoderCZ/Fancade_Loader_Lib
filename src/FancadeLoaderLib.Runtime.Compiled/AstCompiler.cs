using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Runtime.Compiled.Utils;
using FancadeLoaderLib.Runtime.Exceptions;
using FancadeLoaderLib.Runtime.Syntax;
using FancadeLoaderLib.Runtime.Syntax.Control;
using FancadeLoaderLib.Runtime.Syntax.Values;
using FancadeLoaderLib.Runtime.Syntax.Variables;
using MathUtils.Vectors;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.Extensions.ObjectPool;
using System;
using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace FancadeLoaderLib.Runtime.Compiled;

public sealed class AstCompiler
{
    private readonly Environment[] _environments;
    private readonly StringBuilder _writerBuilder;
    private readonly IndentedTextWriter _writer;

    private readonly Dictionary<(int, Variable), string> _varToName = [];

    private readonly ImmutableArray<(int, Variable)> _variables;

    private readonly ObjectPool<IndentedTextWriter> _writerPool = new DefaultObjectPoolProvider().Create(new IndentedTextWriterPoolPolicy());

    private readonly Queue<(EntryPoint EntryPoint, SignalType Type)> _nodesToWrite = [];
    private readonly HashSet<EntryPoint> _writtenNodes = [];

    public AstCompiler(AST ast)
        : this(ast, 4)
    {
    }

    public AstCompiler(AST ast, int maxDepth)
    {
        List<Environment> environments = [];
        List<ImmutableArray<Variable>> variables = [];

        var mainEnvironment = new Environment(ast, 0, -1, ushort3.Zero);
        environments.Add(mainEnvironment);
        variables.Add(mainEnvironment.AST.Variables);

        InitEnvironments(mainEnvironment, environments, variables, maxDepth);

        _environments = [.. environments];
        _variables = [.. variables.SelectMany(arr => arr.Select((var, index) => (index, var)))];

        _writerBuilder = new StringBuilder();
        _writer = new IndentedTextWriter(new StringWriter(_writerBuilder));
    }

    public static string Parse(AST ast)
    {
        // TODO: constant fold

        var compiler = new AstCompiler(ast);

        return compiler.WriteAll();
    }

    public static IAstRunner? Compile(string code, IRuntimeContext ctx)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code, new CSharpParseOptions(languageVersion: LanguageVersion.CSharp13));

        string assemblyName = Path.GetRandomFileName();
        MetadataReference[] references =
        [
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ValueType).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
            MetadataReference.CreateFromFile(typeof(IRuntimeContext).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(int3).Assembly.Location),
            MetadataReference.CreateFromFile(Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "System.Numerics.Vectors.dll")),
            MetadataReference.CreateFromFile(typeof(SignalType).Assembly.Location),
        ];

        CSharpCompilation compilation = CSharpCompilation.Create(
               assemblyName,
               syntaxTrees: [tree],
               references: references,
               options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using (var ms = new MemoryStream())
        {
            // write IL code into memory
            EmitResult result = compilation.Emit(ms);

            if (!result.Success)
            {
                // handle exceptions
                IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                    diagnostic.IsWarningAsError ||
                    diagnostic.Severity == DiagnosticSeverity.Error);

                foreach (Diagnostic diagnostic in failures)
                {
                    Console.Error.WriteLine($"{diagnostic.Id}, {diagnostic.Location}: {diagnostic.GetMessage()}");
                }

                return null;
            }
            else
            {
                // load this 'virtual' DLL so that we can use
                ms.Seek(0, SeekOrigin.Begin);
                Assembly assembly = Assembly.Load(ms.ToArray());

                // create instance of the desired class and call the desired function
                Type type = assembly.GetType("FancadeLoaderLib.Runtime.Compiled.Generated.CompiledAST")!;
                object obj = Activator.CreateInstance(type, [ctx])!;
                return (IAstRunner)obj;
            }
        }
    }

    private string WriteAll()
    {
        _writer.WriteLine("""
            using FancadeLoaderLib.Runtime;
            using MathUtils.Vectors;
            using System;
            using System.Numerics;

            namespace FancadeLoaderLib.Runtime.Compiled.Generated;

            """);

        using (_writer.CurlyIndent("public sealed class CompiledAST : IAstRunner"))
        {
            _writer.WriteLine("""
                private readonly IRuntimeContext _ctx;

                """);

            foreach (var (environmentIndex, variable) in _environments[0].AST.GlobalVariables.Select(var => (-1, var)).Concat(_variables))
            {
                _writer.WriteLine($"""
                    private readonly FcList<{SignalTypeToCSharpName(variable.Type.ToNotPointer())}> {GetVariableName(environmentIndex, variable)} = new();
                    """);
            }

            _writer.WriteLine();

            using (_writer.CurlyIndent("public CompiledAST(IRuntimeContext ctx)"))
            {
                _writer.WriteLine("""
                    _ctx = ctx;
                    """);
            }

            using (_writer.CurlyIndent("public Action RunFrame()"))
            {
                foreach (var environment in _environments)
                {
                    foreach (var entryPoint in environment.AST.NotConnectedVoidInputs)
                    {
                        WriteEntryPoint(new EntryPoint(environment.Index, entryPoint.BlockPosition, entryPoint.TerminalPosition), false, _writer);
                    }
                }

                _writer.WriteLine();
                _writer.WriteLine("""
                    return () => { };
                    """);
            }

            while (_nodesToWrite.TryDequeue(out var item))
            {
                var (entryPoint, type) = item;

                if (!_writtenNodes.Add(entryPoint))
                {
                    continue;
                }

                using (_writer.CurlyIndent($"private {SignalTypeToCSharpName(type)} {GetEntryPointMethodName(entryPoint)}()"))
                {
                    if (type == SignalType.Void)
                    {
                        WriteEntryPoint(entryPoint, true, _writer);
                    } else
                    {
                        _writer.Write("return ");

                        var env = _environments[entryPoint.EnvironmentIndex];
                        WriteExpression(new SyntaxTerminal(env.AST.Nodes[entryPoint.BlockPos], entryPoint.TerminalPos), type.IsPointer(), env, true, _writer);

                        _writer.WriteLine(';');
                    }
                }
            }
        }

        _writer.WriteLine("""
            public sealed class FcList<T> where T : struct
            {
                private static readonly T DefaultValue = typeof(T) == typeof(Quaternion) ? (T)(object)Quaternion.Identity : default;

                private T[] _items;
                private int _count;

                public FcList()
                {
                    _items = [];
                }

                public int Count => _count;

                public T this[int index]
                {
                    get => index >= 0 && index < _count ? _items[index] : DefaultValue;
                    set
                    {
                        if (index < 0)
                        {
                            return;
                        }

                        if (index >= _items.Length)
                        {
                            int newLen = index == 0 ? 1 : _items.Length + 16;

                            if (newLen < index + 1)
                            {
                                newLen = index + 1;
                            }

                            int oldLength = _items.Length;

                            Array.Resize(ref _items, newLen);

                            Array.Fill(_items, DefaultValue, oldLength, newLen - oldLength);
                        }

                        if (index >= _count)
                        {
                            _count = index + 1;
                        }

                        _items[index] = value;
                    }
                }

                public readonly struct Ref
                {
                    private readonly FcList<T>? _list;
                    private readonly int _index;

                    public Ref(FcList<T>? list, int index)
                    {
                        _list = list;
                        _index = index;
                    }

                    public T Value
                    {
                        get => _list is null ? DefaultValue : _list[_index];
                        set
                        {
                            if (_list is not null)
                            {
                                _list[_index] = value;
                            }
                        }
                    }

                    public Ref Add(int value)
                        => new Ref(_list, _index + value);
                }
            }
            """);

        return _writerBuilder.ToString()!;
    }

    private static void InitEnvironments(Environment outer, List<Environment> environments, List<ImmutableArray<Variable>> variables, int maxDepth, int depth = 1)
    {
        if (depth > maxDepth)
        {
            throw new EnvironmentDepthLimitReachedException();
        }

        foreach (var node in outer.AST.Nodes.Values)
        {
            if (node is CustomStatementSyntax customStatement)
            {
                var environment = new Environment(customStatement.AST, environments.Count, outer.Index, customStatement.Position);
                environments.Add(environment);
                variables.Add(environment.AST.Variables);
                outer.BlockData[customStatement.Position] = environment;

                InitEnvironments(environment, environments, variables, maxDepth, depth + 1);
            }
        }
    }

    private void WriteEntryPoint(EntryPoint entryPoint, bool direct, IndentedTextWriter writer)
    {
        Stack<EntryPoint> stack = [];

        stack.Push(entryPoint);

        while (stack.TryPop(out var item))
        {
            var (environmentIndex, pos, terminalPos) = item;

            var environment = _environments[environmentIndex];

            var statement = WriteStatement(pos, terminalPos, environment, direct, writer);

            foreach (var connection in statement.OutVoidConnections)
            {
                if (connection.FromVoxel == TerminalDef.AfterPosition)
                {
                    if (connection.IsToOutside)
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        stack.Push(new(environmentIndex, connection.To, (byte3)connection.ToVoxel));
                    }
                }
            }

            direct = false;
        }
    }

    private void WriteConnected(StatementSyntax statement, byte3 terminalPos, Environment environment, IndentedTextWriter writer)
    {
        foreach (var connection in statement.OutVoidConnections)
        {
            if (connection.FromVoxel == terminalPos)
            {
                if (connection.IsToOutside)
                {
                    throw new NotImplementedException();
                }
                else
                {
                    WriteEntryPoint(new(environment.Index, connection.To, (byte3)connection.ToVoxel), false, writer);
                }
            }
        }
    }

    private StatementSyntax WriteStatement(ushort3 pos, byte3 terminalPos, Environment environment, bool direct, IndentedTextWriter writer)
    {
        var statement = (StatementSyntax)environment.AST.Nodes[pos];

        if (!direct)
        {
            int conToCount = 0;

            foreach (var con in environment.AST.ConnectionsTo[pos])
            {
                if (con.ToVoxel == terminalPos)
                {
                    conToCount++;
                }
            }

            if (conToCount > 1)
            {
                var entryPoint = new EntryPoint(environment.Index, pos, terminalPos);
                writer.WriteLine($"{GetEntryPointMethodName(entryPoint)}();");
                _nodesToWrite.Enqueue((entryPoint, SignalType.Void));
                return statement;
            }
        }

        // faster than switching on type
        switch (statement.PrefabId)
        {
            // **************************************** Control ****************************************
            case 234:
                {
                    Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be valid.");
                    var ifStatement = (IfStatementSyntax)statement;

                    if (ifStatement.Condition is not null)
                    {
                        writer.Write("""
                            if (
                            """);

                        WriteExpression(ifStatement.Condition, false, environment, writer);

                        writer.WriteLine(')');

                        using (writer.CurlyIndent(newLine: false))
                        {
                            WriteConnected(ifStatement, TerminalDef.GetOutPosition(0, 2, 2), environment, writer);
                        }

                        writer.WriteLine("else");

                        using (writer.CurlyIndent())
                        {
                            WriteConnected(ifStatement, TerminalDef.GetOutPosition(1, 2, 2), environment, writer);
                        }
                    }
                }

                break;

            // **************************************** Value ****************************************
            case 16 or 20 or 24 or 28 or 32:
                {
                    Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be valid.");
                    var inspect = (InspectStatementSyntax)statement;
                    if (inspect.Input is not null)
                    {
                        writer.Write("""
                            _ctx.InspectValue(new RuntimeValue(
                            """);

                        var info = WriteExpression(inspect.Input, false, environment, writer);

                        writer.WriteLine($"""
                            ), SignalType.{Enum.GetName(typeof(SignalType), info.Type)}, "{(info.VariableName is null ? string.Empty : info.VariableName)}", {environment.AST.PrefabId}, new ushort3({pos.X}, {pos.Y}, {pos.Z}));
                            """);
                    }
                }

                break;

            // **************************************** Variables ****************************************
            case 428 or 430 or 432 or 434 or 436 or 438:
                {
                    Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(1), $"{nameof(terminalPos)} should be valid.");
                    var setVar = (SetVaribleStatementSyntax)statement;

                    if (setVar.Value is not null)
                    {
                        writer.Write($"""
                            {GetVariableName(environment.Index, setVar.Variable)}[0] = 
                            """);

                        WriteExpression(setVar.Value, false, environment, writer);

                        writer.WriteLine(";");
                    }
                }

                break;
            case 58 or 62 or 66 or 70 or 74 or 78:
                {
                    Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be valid.");
                    var setPointer = (SetPointerStatementSyntax)statement;

                    if (setPointer.Variable is not null && setPointer.Value is not null)
                    {
                        if (!TryWriteDirrectRef(setPointer.Variable, environment, writer))
                        {
                            WriteExpression(setPointer.Variable, true, environment, writer);

                            writer.Write("""
                            .Value
                            """);
                        }

                        writer.Write(" = ");

                        WriteExpression(setPointer.Value, false, environment, writer);

                        writer.WriteLine(';');
                    }
                }

                break;
            case 556 or 558:
                {
                    Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(1), $"{nameof(terminalPos)} should be valid.");
                    var incDecNumber = (IncDecNumberStatementSyntax)statement;

                    if (incDecNumber.Variable is not null)
                    {
                        if (!TryWriteDirrectRef(incDecNumber.Variable, environment, writer))
                        {
                            WriteExpression(incDecNumber.Variable, true, environment, writer);

                            writer.Write(".Value");
                        }

                        writer.WriteLine(incDecNumber.PrefabId switch
                        {
                            556 => "++;",
                            558 => "--;",
                            _ => throw new UnreachableException(),
                        });
                    }
                }

                break;
            default:
                throw new NotImplementedException($"Prefab with id {statement.PrefabId} is not implemented.");
        }

        return statement;
    }

    /*private ExpressionInfo WriteExpression(SyntaxTerminal terminal, SignalType type, IndentedTextWriter writer)
    {
        var poolWriter = _writerPool.Get();

        try
        {
            var info = WriteExpression(terminal, type.IsPointer(), poolWriter);

            var infoType = info.PtrType;
            if (infoType == type)
            {
                writer.Write(poolWriter.InnerWriter.ToString());

                return info;
            }

            switch (infoType.ToNotPointer())
            {
                case SignalType.Float:
                    switch (type.ToNotPointer())
                    {
                        case SignalType.Float:
                            {

                            }

                            break;
                        case SignalType.Vec3:
                            {

                            }

                            break;
                        case SignalType.Rot:
                            {

                            }

                            break;
                        case SignalType.Bool:
                            {

                            }

                            break;
                        case SignalType.Obj or SignalType.Con:
                            {

                            }

                            break;
                        default:
                            throw new UnreachableException();
                    }

                    break;
                case SignalType.Vec3:
                    switch (type.ToNotPointer())
                    {
                        case SignalType.Float:
                            {

                            }

                            break;
                        case SignalType.Vec3:
                            {

                            }

                            break;
                        case SignalType.Rot:
                            {

                            }

                            break;
                        case SignalType.Bool:
                            {

                            }

                            break;
                        case SignalType.Obj or SignalType.Con:
                            {

                            }

                            break;
                        default:
                            throw new UnreachableException();
                    }

                    break;
                case SignalType.Rot:
                    switch (type.ToNotPointer())
                    {
                        case SignalType.Float:
                            {

                            }

                            break;
                        case SignalType.Vec3:
                            {

                            }

                            break;
                        case SignalType.Rot:
                            {

                            }

                            break;
                        case SignalType.Bool:
                            {

                            }

                            break;
                        case SignalType.Obj or SignalType.Con:
                            {

                            }

                            break;
                        default:
                            throw new UnreachableException();
                    }

                    break;
                case SignalType.Bool:
                    switch (type.ToNotPointer())
                    {
                        case SignalType.Float:
                            {

                            }

                            break;
                        case SignalType.Vec3:
                            {

                            }

                            break;
                        case SignalType.Rot:
                            {

                            }

                            break;
                        case SignalType.Bool:
                            {

                            }

                            break;
                        case SignalType.Obj or SignalType.Con:
                            {

                            }

                            break;
                        default:
                            throw new UnreachableException();
                    }

                    break;
                case SignalType.Obj or SignalType.Con:
                    switch (type.ToNotPointer())
                    {
                        case SignalType.Float:
                            {
                                writer.Write("(float)");
                            }

                            break;
                        case SignalType.Vec3:
                            {

                            }

                            break;
                        case SignalType.Rot:
                            {

                            }

                            break;
                        case SignalType.Bool:
                            {

                            }

                            break;
                        case SignalType.Obj or SignalType.Con:
                            break; // both types are int
                        default:
                            throw new UnreachableException();
                    }

                    break;
                default:
                    throw new UnreachableException();
            }

            writer.Write(poolWriter.InnerWriter.ToString());

            if (type.IsPointer() && !infoType.IsPointer())
            {
                // TODO
                throw new Exception();
            }
            else if (!type.IsPointer() && infoType.IsPointer())
            {
                writer.Write(".Value");
            }

            if (type.IsPointer())
            {
                Debug.Assert(info.VariableName is not null);
                return new ExpressionInfo(new Variable(info.VariableName, type));
            }
            else
            {
                return new ExpressionInfo(type);
            }
        }
        finally
        {
            _writerPool.Return(poolWriter);
        }
    }*/

    private ExpressionInfo WriteExpression(SyntaxTerminal terminal, bool asReference, Environment environment, IndentedTextWriter writer)
        => WriteExpression(terminal, asReference, environment, false, writer);

    private ExpressionInfo WriteExpression(SyntaxTerminal terminal, bool asReference, Environment environment, bool direct, IndentedTextWriter writer)
    {
        if (!direct)
        {
            int conFromCount = 0;

            foreach (var con in environment.AST.ConnectionsFrom[terminal.Node.Position])
            {
                if (con.FromVoxel == terminal.Position)
                {
                    conFromCount++;
                }
            }

            if (conFromCount > 1)
            {
                var entryPoint = new EntryPoint(environment.Index, terminal.Node.Position, terminal.Position);
                writer.Write($"{GetEntryPointMethodName(entryPoint)}()");
                ExpressionInfo info = GetExpressionInfo(terminal, asReference);
                _nodesToWrite.Enqueue((entryPoint, info.PtrType));
                return info;
            }
        }

        // faster than switching on type
        switch (terminal.Node.PrefabId)
        {
            // **************************************** Value ****************************************
            case 36 or 38 or 42 or 449 or 451:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(0, 2, terminal.Node.PrefabId is 38 or 42 ? 2 : 1), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    Debug.Assert(!asReference);
                    var literal = (LiteralExpressionSyntax)terminal.Node;

                    switch (terminal.Node.PrefabId)
                    {
                        case 36:
                            writer.Write($"{ToString(literal.Value.Float)}f");
                            return new ExpressionInfo(SignalType.Float);
                        case 38:
                            var vec = literal.Value.Float3;
                            writer.Write($"new float3({ToString(vec.X)}f, {ToString(vec.Y)}f, {ToString(vec.Z)}f)");
                            return new ExpressionInfo(SignalType.Vec3);
                        case 42:
                            var rot = literal.Value.Quaternion;
                            writer.Write($"new Quaternion({ToString(rot.X)}f, {ToString(rot.Y)}f, {ToString(rot.Z)}f, {ToString(rot.W)}f)");
                            return new ExpressionInfo(SignalType.Rot);
                        case 449 or 451:
                            writer.Write(literal.Value.Bool ? "true" : "false");
                            return new ExpressionInfo(SignalType.Bool);
                        default:
                            throw new UnreachableException();
                    }
                }

            // **************************************** Variables ****************************************
            case 46 or 48 or 50 or 52 or 54 or 56:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(0, 2, 1), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    var getVariable = (GetVariableExpressionSyntax)terminal.Node;

                    if (asReference)
                    {
                        writer.Write($"""
                            new FcList<{SignalTypeToCSharpName(getVariable.Variable.Type.ToNotPointer())}>.Ref({GetVariableName(environment.Index, getVariable.Variable)}, 0)
                            """);
                    }
                    else
                    {
                        writer.Write($"""
                            {GetVariableName(environment.Index, getVariable.Variable)}[0]
                            """);
                    }

                    return new ExpressionInfo(getVariable.Variable);
                }

            case 82 or 461 or 465 or 469 or 86 or 473:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(0, 2, 2), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    var list = (ListExpressionSyntax)terminal.Node;

                    var type = terminal.Node.PrefabId switch
                    {
                        82 => SignalType.Float,
                        461 => SignalType.Vec3,
                        465 => SignalType.Rot,
                        469 => SignalType.Bool,
                        86 => SignalType.Obj,
                        473 => SignalType.Con,
                        _ => throw new UnreachableException(),
                    };

                    if (list.Variable is null)
                    {
                        if (asReference)
                        {
                            writer.Write($"""
                                new FcList<{SignalTypeToCSharpName(type)}>.Ref(null, 0)
                                """);
                        }
                        else
                        {
                            writer.Write(SignalTypeToDefaultValue(type));
                        }

                        return new ExpressionInfo(type);
                    }

                    if (list.Index is null)
                    {
                        return WriteExpression(list.Variable, asReference, environment, writer);
                    }
                    else if (list.Variable.Node is GetVariableExpressionSyntax getVariable)
                    {
                        if (asReference)
                        {
                            writer.Write($"""
                                new FcList<{SignalTypeToCSharpName(getVariable.Variable.Type.ToNotPointer())}>.Ref({GetVariableName(environment.Index, getVariable.Variable)}, (int)
                                """);

                            WriteExpression(list.Index, false, environment, writer);

                            writer.Write(')');
                        }
                        else
                        {
                            writer.Write($"""
                                {GetVariableName(environment.Index, getVariable.Variable)}[(int)
                                """);

                            WriteExpression(list.Index, false, environment, writer);

                            writer.Write(']');
                        }

                        return new ExpressionInfo(getVariable.Variable);
                    }

                    var varInfo = WriteExpression(list.Variable, true, environment, writer);

                    writer.Write("""
                            .Add((int)
                            """);

                    WriteExpression(list.Index, false, environment, writer);

                    writer.Write(")");

                    if (!asReference)
                    {
                        writer.Write(".Value");
                    }

                    return varInfo;
                }

            default:
                throw new NotImplementedException($"Prefab with id {terminal.Node.PrefabId} is not implemented.");
        }
    }

    private static ExpressionInfo GetExpressionInfo(SyntaxTerminal terminal, bool asReference)
    {
        // faster than switching on type
        switch (terminal.Node.PrefabId)
        {
            // **************************************** Value ****************************************
            case 36 or 38 or 42 or 449 or 451:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(0, 2, terminal.Node.PrefabId is 38 or 42 ? 2 : 1), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    Debug.Assert(terminal.Node is LiteralExpressionSyntax);
                    Debug.Assert(!asReference);

                    return terminal.Node.PrefabId switch
                    {
                        36 => new ExpressionInfo(SignalType.Float),
                        38 => new ExpressionInfo(SignalType.Vec3),
                        42 => new ExpressionInfo(SignalType.Rot),
                        449 or 451 => new ExpressionInfo(SignalType.Bool),
                        _ => throw new UnreachableException(),
                    };
                }

            // **************************************** Variables ****************************************
            case 46 or 48 or 50 or 52 or 54 or 56:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(0, 2, 1), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    var getVariable = (GetVariableExpressionSyntax)terminal.Node;

                    return new ExpressionInfo(getVariable.Variable);
                }

            case 82 or 461 or 465 or 469 or 86 or 473:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(0, 2, 2), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    var list = (ListExpressionSyntax)terminal.Node;

                    var type = terminal.Node.PrefabId switch
                    {
                        82 => SignalType.Float,
                        461 => SignalType.Vec3,
                        465 => SignalType.Rot,
                        469 => SignalType.Bool,
                        86 => SignalType.Obj,
                        473 => SignalType.Con,
                        _ => throw new UnreachableException(),
                    };

                    if (list.Variable is null)
                    {
                        return new ExpressionInfo(type);
                    }

                    if (list.Index is null)
                    {
                        return GetExpressionInfo(list.Variable, asReference);
                    }
                    else if (list.Variable.Node is GetVariableExpressionSyntax getVariable)
                    {
                        return new ExpressionInfo(getVariable.Variable);
                    }

                    var varInfo = GetExpressionInfo(list.Variable, true);

                    return varInfo;
                }

            default:
                throw new NotImplementedException($"Prefab with id {terminal.Node.PrefabId} is not implemented.");
        }
    }

    private bool TryWriteDirrectRef(SyntaxTerminal terminal, Environment environment, IndentedTextWriter writer)
    {
        switch (terminal.Node.PrefabId)
        {
            case 46 or 48 or 50 or 52 or 54 or 56:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(0, 2, 1), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    var getVariable = (GetVariableExpressionSyntax)terminal.Node;

                    writer.Write($"""
                            {GetVariableName(environment.Index, getVariable.Variable)}[0]
                            """);

                    return true;
                }

            case 82 or 461 or 465 or 469 or 86 or 473:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(0, 2, 2), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    var list = (ListExpressionSyntax)terminal.Node;

                    if (list.Variable is null)
                    {
                        return false;
                    }

                    if (list.Index is null)
                    {
                        return TryWriteDirrectRef(list.Variable, environment, writer);
                    }
                    else if (list.Variable.Node is GetVariableExpressionSyntax getVariable)
                    {
                        writer.Write($"""
                            {GetVariableName(environment.Index, getVariable.Variable)}[(int)
                            """);

                        WriteExpression(list.Index, false, environment, writer);

                        writer.Write(']');

                        return true;
                    }

                    return false;
                }

            default:
                return false;
        }
    }

    private string GetVariableName(int environmentIndex, Variable variable)
    {
        if (_varToName.TryGetValue((environmentIndex, variable), out string? name))
        {
            return name;
        }

        name = string.Create(variable.Name.Length + 2 + (variable.IsGlobal ? 1 : 0) + (environmentIndex == -1 ? 0 : IntLength(environmentIndex) + 1), (environmentIndex, variable), (span, item) =>
        {
            var (environmentIndex, variable) = item;

            ReadOnlySpan<char> varName = variable.Name;

            if (variable.Name.StartsWith('$'))
            {
                span[0] = 'g';
                span[1] = '_';
                span = span[2..];
                varName = varName[1..];
            }
            else if (variable.Name.StartsWith('!'))
            {
                span[0] = 's';
                span[1] = '_';
                span = span[2..];
                varName = varName[1..];
            }

            span[0] = variable.Type.ToNotPointer() switch
            {
                SignalType.Float => 'f',
                SignalType.Vec3 => 'v',
                SignalType.Rot => 'r',
                SignalType.Bool => 'b',
                SignalType.Obj => 'o',
                SignalType.Con => 'c',
                _ => throw new UnreachableException(),
            };
            span[1] = '_';
            span = span[2..];

            if (environmentIndex != -1)
            {
                bool written = environmentIndex.TryFormat(span, out int numbWritten);

                Debug.Assert(written);

                span[numbWritten] = '_';

                span = span[(numbWritten + 1)..];
            }

            varName.CopyTo(span);
        });

        _varToName[(environmentIndex, variable)] = name;

        return name;
    }

    private static string SignalTypeToCSharpName(SignalType type)
        => type.ToNotPointer() switch
        {
            SignalType.Void => "void",
            SignalType.Float => "float",
            SignalType.FloatPtr => "FcList<float>.Ref",
            SignalType.Vec3 => "float3",
            SignalType.Vec3Ptr => "FcList<float3>.Ref",
            SignalType.Rot => "Quaternion",
            SignalType.RotPtr => "FcList<Quaternion>.Ref",
            SignalType.Bool => "bool",
            SignalType.BoolPtr => "FcList<bool>.Ref",
            SignalType.Obj => "int",
            SignalType.ObjPtr => "FcList<int>.Ref",
            SignalType.Con => "int",
            SignalType.ConPtr => "FcList<int>.Ref",
            _ => throw new UnreachableException(),
        };

    private static string SignalTypeToDefaultValue(SignalType type)
        => type.ToNotPointer() switch
        {
            SignalType.Float => "0f",
            SignalType.Vec3 => "float3.Zero",
            SignalType.Rot => "Quaternion.Identity",
            SignalType.Bool => "false",
            SignalType.Obj => "0",
            SignalType.Con => "0",
            _ => throw new UnreachableException(),
        };

    private static string ToString(float value)
        => value.ToString("G9", CultureInfo.InvariantCulture);

    private static string GetEntryPointMethodName(EntryPoint entryPoint)
        => $"Run{entryPoint.EnvironmentIndex}_{entryPoint.BlockPos.X}_{entryPoint.BlockPos.Y}_{entryPoint.BlockPos.Z}__{entryPoint.TerminalPos.X}_{entryPoint.TerminalPos.Y}_{entryPoint.TerminalPos.Z}";

    public static int IntLength(int i)
        => i switch
        {
            < 0 => (int)Math.Floor(Math.Log10(-i)) + 2,
            0 => 1,
            _ => (int)Math.Floor(Math.Log10(i)) + 1,
        };

    private readonly struct ExpressionInfo
    {
        public readonly SignalType Type;
        public readonly string? VariableName;

        public ExpressionInfo(SignalType type)
        {
            Type = type.ToNotPointer();
        }

        public ExpressionInfo(Variable variable)
        {
            Type = variable.Type.ToNotPointer();
            VariableName = variable.Name;
        }

        public bool IsPointer => VariableName is not null;

        public SignalType PtrType => IsPointer ? Type.ToPointer() : Type;
    }

    private readonly struct EntryPoint
    {
        public readonly int EnvironmentIndex;
        public readonly ushort3 BlockPos;
        public readonly byte3 TerminalPos;

        public EntryPoint(int environmentIndex, ushort3 blockPos, byte3 terminalPos)
        {
            EnvironmentIndex = environmentIndex;
            BlockPos = blockPos;
            TerminalPos = terminalPos;
        }

        public void Deconstruct(out int environmentIndex, out ushort3 blockPos, out byte3 terminalPos)
        {
            environmentIndex = EnvironmentIndex;
            blockPos = BlockPos;
            terminalPos = TerminalPos;
        }
    }

    private sealed class Environment
    {
        public Environment(AST ast, int index, int outerEnvironmentIndex, ushort3 outerPosition)
        {
            Index = index;
            OuterEnvironmentIndex = outerEnvironmentIndex;
            AST = ast;
            OuterPosition = outerPosition;
        }

        public AST AST { get; }

        public int Index { get; }

        public int OuterEnvironmentIndex { get; }

        public ushort3 OuterPosition { get; }

        public Dictionary<ushort3, object> BlockData { get; } = [];
    }

    private class IndentedTextWriterPoolPolicy : PooledObjectPolicy<IndentedTextWriter>
    {
        public override IndentedTextWriter Create()
            => new IndentedTextWriter(new StringWriter());

        public override bool Return(IndentedTextWriter obj)
        {
            ((StringWriter)obj.InnerWriter).GetStringBuilder().Clear();

            return true;
        }
    }
}
