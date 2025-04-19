using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Runtime.Compiled.Utils;
using FancadeLoaderLib.Runtime.Syntax;
using FancadeLoaderLib.Runtime.Syntax.Values;
using FancadeLoaderLib.Runtime.Syntax.Variables;
using MathUtils.Vectors;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace FancadeLoaderLib.Runtime.Compiled;

public sealed class AstCompiler
{
    private readonly AST _ast;
    private readonly StringBuilder _writerBuilder;
    private readonly IndentedTextWriter _writer;

    private readonly Dictionary<Variable, string> _varToName = [];

    public AstCompiler(AST ast)
    {
        _ast = ast;

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

            foreach (var variable in _ast.GlobalVariables.Concat(_ast.Variables))
            {
                _writer.WriteLine($"""
                    private readonly FcList<{SignalTypeToCSharpName(variable.Type)}> {GetVariableName(variable)} = new();
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
                WriteEntryPoint(_ast.NotConnectedVoidInputs.First());

                _writer.WriteLine("""
                    return () => { };
                    """);
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
            }
            """);

        return _writerBuilder.ToString()!;
    }

    private void WriteEntryPoint((ushort3 Pos, byte3 TerminalPos) entryPoint)
    {
        Stack<(ushort3, byte3)> stack = [];

        stack.Push(entryPoint);

        while (stack.TryPop(out var item))
        {
            var (pos, terminalPos) = item;

            var statement = WriteStatement(pos, terminalPos, _writer);

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
                        stack.Push(new(connection.To, (byte3)connection.ToVoxel));
                    }
                }
            }
        }
    }

    private StatementSyntax WriteStatement(ushort3 pos, byte3 terminalPos, IndentedTextWriter writer)
    {
        var statement = (StatementSyntax)_ast.Nodes[pos];

        // faster than switching on type
        switch (statement.PrefabId)
        {
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

                        var info = WriteExpression(inspect.Input, false, writer);

                        writer.WriteLine($"""
                            ), SignalType.{Enum.GetName(typeof(SignalType), info.Type)}, "{(info.Variable is null ? string.Empty : info.Variable.Value.Name)}", {_ast.PrefabId}, new ushort3({pos.X}, {pos.Y}, {pos.Z}));
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
                            {GetVariableName(setVar.Variable)}[0] = 
                            """);

                        WriteExpression(setVar.Value, false, writer);

                        writer.WriteLine(";");
                    }
                }

                break;
            default:
                throw new NotImplementedException($"Prefab with id {statement.PrefabId} is not implemented.");
        }

        return statement;
    }

    private ExpressionInfo WriteExpression(SyntaxTerminal terminal, bool asReference, IndentedTextWriter writer)
    {
        switch (terminal.Node.PrefabId)
        {
            // **************************************** Value ****************************************
            case 36 or 38 or 42 or 449 or 451:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(0, 2, terminal.Node.PrefabId is 38 or 42 ? 2 : 1), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
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
                    var getVar = (GetVariableExpressionSyntax)terminal.Node;

                    if (asReference)
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        writer.Write($"""
                            {GetVariableName(getVar.Variable)}[0]
                            """);
                    }

                    return new ExpressionInfo(getVar.Variable);
                }

            default:
                throw new NotImplementedException($"Prefab with id {terminal.Node.PrefabId} is not implemented.");
        }
    }

    private string GetVariableName(Variable variable)
    {
        if (_varToName.TryGetValue(variable, out string? name))
        {
            return name;
        }

        name = string.Create(variable.Name.Length + 2 + (variable.IsGlobal ? 1 : 0), variable, (span, variable) =>
        {
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

            varName.CopyTo(span);
        });

        _varToName[variable] = name;

        return name;
    }

    private string SignalTypeToCSharpName(SignalType type)
        => type.ToNotPointer() switch
        {
            SignalType.Float => "float",
            SignalType.Vec3 => nameof(float3),
            SignalType.Rot => nameof(Quaternion),
            SignalType.Bool => "bool",
            SignalType.Obj => "int",
            SignalType.Con => "int",
            _ => throw new UnreachableException(),
        };

    private static string ToString(float value)
        => value.ToString("G9", CultureInfo.InvariantCulture);

    private readonly struct ExpressionInfo
    {
        public readonly SignalType Type;
        public readonly Variable? Variable;

        public ExpressionInfo(SignalType type)
        {
            Type = type;
            Variable = null;
        }

        public ExpressionInfo(Variable variable)
        {
            Type = variable.Type;
            Variable = variable;
        }
    }
}
