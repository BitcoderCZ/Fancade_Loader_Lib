using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Fancade.Editing.Scripting.Settings;
using BitcoderCZ.Fancade.Runtime.Compiled.Utils;
using BitcoderCZ.Fancade.Runtime.Exceptions;
using BitcoderCZ.Fancade.Runtime.Syntax;
using BitcoderCZ.Fancade.Runtime.Syntax.Variables;
using BitcoderCZ.Maths.Vectors;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.Extensions.ObjectPool;
using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace BitcoderCZ.Fancade.Runtime.Compiled;

public sealed partial class AstCompiler
{
    private readonly Environment[] _environments;
    private readonly StringBuilder _writerBuilder;
    private readonly IndentedTextWriter _writer;

    private readonly TimeSpan _timeout;

    private readonly Dictionary<(int, Variable), string> _varToName = [];

    private readonly ImmutableArray<(int, Variable)> _variables;

    private readonly ObjectPool<IndentedTextWriter> _writerPool = new DefaultObjectPoolProvider().Create(new IndentedTextWriterPoolPolicy());

    private readonly Queue<(SyntaxTerminal Terminal, int EnvironmentIndex, SignalType Type)> _nodesToWrite = [];
    private readonly HashSet<(EntryPoint EntryPoint, bool IsPtr)> _writtenNodes = [];

    private readonly HashSet<(string Name, string Type, string? DefaultValue)> _stateStoreVariables = [];

    private int _localVarCounter = 0;

    private AstCompiler(AST ast, TimeSpan timeout, int maxDepth)
    {
        _timeout = timeout;

        List<Environment> environments = [];
        List<ImmutableArray<Variable>> variables = [];

        var mainEnvironment = new Environment(ast, 0, -1, ushort3.Zero);
        environments.Add(mainEnvironment);
        variables.Add(mainEnvironment.AST.Variables);

        InitEnvironments(mainEnvironment, environments, variables, maxDepth);

        _environments = [.. environments];
        _variables = [.. variables.Select((var, index) => (index, var)).SelectMany(item => item.var.Select(var => (item.index, var)))];

        _writerBuilder = new StringBuilder();
        _writer = new IndentedTextWriter(new StringWriter(_writerBuilder));
    }

    public static string Parse(AST ast)
        => Parse(ast, TimeSpan.FromSeconds(3));

    public static string Parse(AST ast, TimeSpan timeout)
        => Parse(ast, timeout, 4);

    public static string Parse(AST ast, TimeSpan timeout, int maxDepth)
    {
        // TODO: constant fold

        var compiler = new AstCompiler(ast, timeout, maxDepth);

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
            MetadataReference.CreateFromFile(typeof(Ranking).Assembly.Location),
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

                    int start = Math.Max(diagnostic.Location.SourceSpan.Start - 5, 0);
                    int end = Math.Min(diagnostic.Location.SourceSpan.End + 5, code.Length - 1);
                    Console.Error.WriteLine(code.AsSpan()[start..end]);
                }

                return null;
            }
            else
            {
                // load this 'virtual' DLL so that we can use
                ms.Seek(0, SeekOrigin.Begin);
                Assembly assembly = Assembly.Load(ms.ToArray());

                // create instance of the desired class and call the desired function
                Type type = assembly.GetType("BitcoderCZ.Fancade.Runtime.Compiled.Generated.CompiledAST`1")!.MakeGenericType([ctx.GetType()]);
                object obj = Activator.CreateInstance(type, [ctx])!;
                return (IAstRunner)obj;
            }
        }
    }

    private string WriteAll()
    {
        _writer.WriteLine("""
            using BitcoderCZ.Fancade.Editing;
            using BitcoderCZ.Fancade.Editing.Scripting.Settings;
            using BitcoderCZ.Fancade.Runtime;
            using BitcoderCZ.Maths.Vectors;
            using System;
            using System.Collections.Generic;
            using System.Diagnostics;
            using System.Numerics;
            using System.Runtime.CompilerServices;

            namespace BitcoderCZ.Fancade.Runtime.Compiled.Generated;

            """);

        using (_writer.CurlyIndent("public sealed class CompiledAST<TRuntimeContext> : IAstRunner where TRuntimeContext : IRuntimeContext"))
        {
            _writer.WriteLineAll("""
                private readonly TRuntimeContext _ctx;
                
                private Queue<Action>? lateUpdateQueue = new();
                
                """);

            if (_timeout != Timeout.InfiniteTimeSpan)
            {
                _writer.WriteLineAllInv($"""
                    private readonly TimeSpan _timeout = new TimeSpan({_timeout.Ticks});
                    private readonly Stopwatch _timeoutWatch = new();
                    
                    """);
            }

            foreach (var (environmentIndex, variable) in _environments[0].AST.GlobalVariables.Select(var => (-1, var)).Concat(_variables))
            {
                _writer.WriteLineInv($"""
                    private readonly FcList<{GetCSharpName(variable.Type.ToNotPointer())}> {GetVariableName(environmentIndex, variable)} = new();
                    """);
            }

            _writer.WriteLine();

            _writer.WriteLine("public IEnumerable<Variable> GlobalVariables =>");
            _writer.WriteLine('[');

            _writer.Indent++;
            foreach (var variable in _environments[0].AST.GlobalVariables)
            {
                _writer.WriteLineInv($"""
                    new Variable("{variable.Name}", SignalType.{variable.Type}),
                    """);
            }

            _writer.Indent--;

            _writer.WriteLine("];");

            _writer.WriteLine();

            using (_writer.CurlyIndent("public CompiledAST(TRuntimeContext ctx)"))
            {
                _writer.WriteLine("""
                    _ctx = ctx;
                    """);
            }

            using (_writer.CurlyIndent("public Action RunFrame()"))
            {
                if (_timeout != Timeout.InfiniteTimeSpan)
                {
                    _writer.WriteLineAll("""  
                        if (_timeoutWatch.IsRunning)
                        {
                            throw new InvalidOperationException("This method cannot be called concurrently.");
                        }

                        _timeoutWatch.Start();

                        """);
                }

                foreach (var environment in _environments)
                {
                    foreach (var entryPoint in environment.AST.NotConnectedVoidInputs)
                    {
                        WriteEntryPoint(new EntryPoint(environment.Index, entryPoint.BlockPosition, entryPoint.TerminalPosition), false, _writer);
                    }
                }

                if (_timeout == Timeout.InfiniteTimeSpan)
                {
                    _writer.WriteLineAll("""

                        return () =>
                        { 
                            var queue = lateUpdateQueue;
                            lateUpdateQueue = null;

                            while (queue.TryDequeue(out var lateUpdate))
                            {
                                lateUpdate();
                            }

                            lateUpdateQueue = queue;
                        };
                        """);
                }
                else
                {
                    _writer.WriteLineAll("""
                        
                        return () =>
                        { 
                            if (!_timeoutWatch.IsRunning)
                            {
                                throw new InvalidOperationException("This method cannot be called concurrently.");
                            }
                        
                            _timeoutWatch.Restart();

                            var queue = lateUpdateQueue;
                            lateUpdateQueue = null;

                            while (queue.TryDequeue(out var lateUpdate))
                            {
                                lateUpdate();
                            }

                            lateUpdateQueue = queue;
                            _timeoutWatch.Reset();
                        };
                        """);
                }
            }

            using (_writer.CurlyIndent("public Span<RuntimeValue> GetGlobalVariableValue(Variable variable)"))
            {
                using (_writer.CurlyIndent("switch (variable.Name)"))
                {
                    foreach (var grouping in _environments[0].AST.GlobalVariables.GroupBy(variable => variable.Name))
                    {
                        _writer.WriteLineInv($"""
                            case "{grouping.Key}":
                            """);

                        _writer.Indent++;
                        using (_writer.CurlyIndent("switch (variable.Type)"))
                        {
                            foreach (var variable in grouping)
                            {
                                _writer.WriteLineInv($"""
                            case SignalType.{variable.Type}:
                            """);

                                _writer.Indent++;
                                _writer.WriteLineInv($"return {GetVariableName(-1, variable)}.AsSpan();");
                                _writer.Indent--;
                            }
                        }

                        _writer.WriteLine("break;");
                        _writer.Indent--;
                    }
                }

                _writer.WriteLine("return [];");
            }

            if (_timeout != Timeout.InfiniteTimeSpan)
            {
                _writer.WriteLineAll("""
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    private void ThrowIfTimeout()
                    {
                        if (_timeoutWatch.Elapsed > _timeout)
                        {
                            throw new TimeoutException();
                        }
                    }

                    """);
            }

            while (_nodesToWrite.TryDequeue(out var item))
            {
                var (terminal, environmentIndex, type) = item;

                var entryPoint = new EntryPoint(environmentIndex, terminal.Node.Position, terminal.Position);

                if (!_writtenNodes.Add((entryPoint, type.IsPointer())))
                {
                    continue;
                }

                using (_writer.CurlyIndent($"private {GetCSharpName(type)} {GetEntryPointMethodName(entryPoint, type != SignalType.Void && type.IsPointer())}()"))
                {
                    if (_timeout != Timeout.InfiniteTimeSpan)
                    {
                        _writer.WriteLine("""
                            ThrowIfTimeout();

                            """);
                    }

                    if (type == SignalType.Void)
                    {
                        WriteEntryPoint(entryPoint, true, _writer);
                    }
                    else
                    {
                        _writer.Write("return ");

                        var env = _environments[environmentIndex];
                        WriteExpression(terminal, type.IsPointer(), env, true, _writer);

                        _writer.WriteLine(';');
                    }
                }
            }

            foreach (var (varName, type, defaultValue) in _stateStoreVariables)
            {
                _writer.WriteLineInv($"private {type} {varName}{(defaultValue is null ? string.Empty : $"= {defaultValue}")};");
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

                public Span<RuntimeValue> AsSpan()
                {
                    var result = new RuntimeValue[_count];

                    if (typeof(T) == typeof(float))
                    {
                        for (int i = 0; i < _count; i++)
                        {
                            result[i] = new RuntimeValue((float)(object)_items[i]);
                        }
                    }
                    else if (typeof(T) == typeof(float3))
                    {
                        for (int i = 0; i < _count; i++)
                        {
                            result[i] = new RuntimeValue((float3)(object)_items[i]);
                        }
                    }
                    else if (typeof(T) == typeof(Quaternion))
                    {
                        for (int i = 0; i < _count; i++)
                        {
                            result[i] = new RuntimeValue((Quaternion)(object)_items[i]);
                        }
                    }
                    else if (typeof(T) == typeof(bool))
                    {
                        for (int i = 0; i < _count; i++)
                        {
                            result[i] = new RuntimeValue((bool)(object)_items[i]);
                        }
                    }
                    else if (typeof(T) == typeof(int))
                    {
                        for (int i = 0; i < _count; i++)
                        {
                            result[i] = new RuntimeValue((int)(object)_items[i]);
                        }
                    }

                    return result;
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

            internal static class NumberUtils
            {
                public static float FcMod(float a, float b)
                {
                    float res = a % b;

                    if (res >= 0f)
                    {
                        return res;
                    }
                    else
                    {
                        return b + res;
                    }
                }
            }

            internal static class VectorUtils
            {
                private const float DegToRad = MathF.PI / 180f;

                public static Vector3 ToNumerics(this float3 value)
                    => new Vector3(value.X, value.Y, value.Z);

                public static float3 ToFloat3(this Vector3 value)
                    => new float3(value.X, value.Y, value.Z);

                public static Quaternion ToQuatDeg(this float3 value)
                    => Quaternion.CreateFromYawPitchRoll(value.Y * DegToRad, value.X * DegToRad, value.Z * DegToRad);

                public static float3 LineVsPlane(Vector3 lineFrom, Vector3 lineTo, Vector3 planePoint, Vector3 planeNormal)
                {
                    float t = Vector3.Dot(planePoint - lineFrom, planeNormal) / Vector3.Dot(lineTo - lineFrom, planeNormal);
                    return (lineFrom + (t * (lineTo - lineFrom))).ToFloat3();
                }
            }

            internal static class QuaternionUtils
            {
                public static Quaternion AxisAngle(Vector3 axis, float angle)
                {
                    angle = angle * (MathF.PI / 180f);

            #if NET6_0_OR_GREATER
                    var (sin, cos) = MathF.SinCos(angle * 0.5f);
            #else
                    float sin = MathF.Sin(angle * 0.5f);
                    float cos = MathF.Cos(angle * 0.5f);
            #endif

                    return Quaternion.Normalize(new Quaternion(axis.X * sin, axis.Y * sin, axis.Z * sin, cos));
                }

                public static Quaternion LookRotation(Vector3 forward, Vector3 up)
                {
                    if (forward == Vector3.Zero)
                    {
                        return Quaternion.Identity;
                    }

                    forward = Vector3.Normalize(forward);
                    up = Vector3.Normalize(up);

                    Vector3 right = Vector3.Cross(up, forward);
                    if (right == Vector3.Zero)
                    {
                        right = Vector3.UnitX;
                    }
                    else
                    {
                        right = Vector3.Normalize(right);
                    }

                    up = Vector3.Cross(forward, right);

            #pragma warning disable SA1117 // Parameters should be on same line or separate lines
                    Matrix4x4 rotationMatrix = new Matrix4x4(
                        right.X, right.Y, right.Z, 0,
                        up.X, up.Y, up.Z, 0,
                        forward.X, forward.Y, forward.Z, 0,
                        0, 0, 0, 1);
            #pragma warning restore SA1117 // Parameters should be on same line or separate lines

                    return Quaternion.CreateFromRotationMatrix(rotationMatrix);
                }

                public static float GetEulerX(this Quaternion rot)
                {
                    float pitchSin = 2.0f * ((rot.W * rot.Y) - (rot.Z * rot.X));

                    if (pitchSin > 1.0f)
                    {
                        return 90f;
                    }
                    else if (pitchSin < -1.0f)
                    {
                        return -90f;
                    }
                    else
                    {
                        return MathF.Asin(pitchSin) * (180f / MathF.PI);
                    }
                }

                public static float GetEulerY(this Quaternion rot)
                {
                    float xx = rot.X * rot.X;
                    float yy = rot.Y * rot.Y;
                    float zz = rot.Z * rot.Z;
                    float ww = rot.W * rot.W;

                    return MathF.Atan2(2.0f * ((rot.Y * rot.Z) + (rot.W * rot.X)), ww + xx - yy - zz) * (180f / MathF.PI);
                }
            
                public static float GetEulerZ(this Quaternion rot)
                {
                    float xx = rot.X * rot.X;
                    float yy = rot.Y * rot.Y;
                    float zz = rot.Z * rot.Z;
                    float ww = rot.W * rot.W;

                    return MathF.Atan2(2.0f * ((rot.X * rot.Y) + (rot.W * rot.Z)), ww - xx - yy + zz) * (180f / MathF.PI);
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

        foreach (var statement in outer.AST.Statements.Values)
        {
            if (statement is CustomStatementSyntax customStatement)
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
        Queue<EntryPoint> queue = [];

        queue.Enqueue(entryPoint);

        while (queue.TryDequeue(out var item))
        {
            var (environmentIndex, pos, terminalPos) = item;

            var environment = _environments[environmentIndex];

            if (!direct)
            {
                int conToCount = 0;

                if (environment.AST.ConnectionsTo.TryGetValue(pos, out var connectionsTo))
                {
                    foreach (var con in connectionsTo)
                    {
                        if (con.ToVoxel == terminalPos)
                        {
                            conToCount++;
                        }
                    }
                }

                if (conToCount > 1)
                {
                    writer.WriteLineInv($"{GetEntryPointMethodName(item, false)}();");
                    _nodesToWrite.Enqueue((new SyntaxTerminal(environment.AST.Statements[pos], terminalPos), environmentIndex, SignalType.Void));
                    continue;
                }
            }

            var statement = WriteStatement(pos, terminalPos, environment, out byte3 executeNext, writer);

            VisitConnected(statement, executeNext, environment, queue.Enqueue);

            direct = false;
        }
    }

    private void WriteConnected(StatementSyntax statement, byte3 terminalPos, Environment environment, IndentedTextWriter writer)
        => VisitConnected(statement, terminalPos, environment, entryPont => WriteEntryPoint(entryPont, false, writer));

    private void VisitConnected(StatementSyntax statement, byte3 terminalPos, Environment environment, Action<EntryPoint> action)
    {
        foreach (var connection in statement.OutVoidConnections)
        {
            if (connection.FromVoxel == terminalPos)
            {
                if (connection.IsToOutside)
                {
                    var outerEnvironment = _environments[environment.OuterEnvironmentIndex];

                    VisitConnected(outerEnvironment.AST.Statements[environment.OuterPosition], (byte3)connection.ToVoxel, outerEnvironment, action);
                }
                else
                {
                    action(new(environment.Index, connection.To, (byte3)connection.ToVoxel));
                }
            }
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

                    writer.WriteInv($"""
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
                        writer.WriteInv($"""
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

        name = string.Create(variable.Name.Length + 2 + (variable.IsGlobal ? 1 : IntLength(environmentIndex) + 1), (environmentIndex, variable), (span, item) =>
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

            if (!variable.IsGlobal)
            {
                bool written = environmentIndex.TryFormat(span, out int numbWritten);

                Debug.Assert(written);

                span[numbWritten] = '_';

                span = span[(numbWritten + 1)..];
            }

            varName.CopyTo(span);

            for (int i = 0; i < span.Length; i++)
            {
                if (char.IsWhiteSpace(span[i]))
                {
                    span[i] = '_';
                }
            }
        });

        _varToName[(environmentIndex, variable)] = name;

        return name;
    }

    private static string GetStateStoreVarName(int environmentIndex, ushort3 blockPos, string suffix)
        => $"store_{environmentIndex}_{blockPos.X}_{blockPos.Y}_{blockPos.Z}_{suffix}";

    private static string GetCSharpName(SignalType type)
        => type switch
        {
            SignalType.Void => "void",
            SignalType.Float => "float",
            SignalType.FloatPtr => "FcList<float>.Ref",
            SignalType.Vec3 => nameof(float3),
            SignalType.Vec3Ptr => $"FcList<{nameof(float3)}>.Ref",
            SignalType.Rot => nameof(Quaternion),
            SignalType.RotPtr => $"FcList<{nameof(Quaternion)}>.Ref",
            SignalType.Bool => "bool",
            SignalType.BoolPtr => "FcList<bool>.Ref",
            SignalType.Obj => nameof(FcObject),
            SignalType.ObjPtr => $"FcList<{nameof(FcObject)}>.Ref",
            SignalType.Con => nameof(FcConstraint),
            SignalType.ConPtr => $"FcList<{nameof(FcConstraint)}>.Ref",
            _ => throw new UnreachableException(),
        };

    private static string GetDefaultValue(SignalType type)
        => type switch
        {
            SignalType.Float => "0f",
            SignalType.FloatPtr => "new FcList<float>.Ref(null, 0)",
            SignalType.Vec3 => $"{nameof(float3)}.{nameof(float3.Zero)}",
            SignalType.Vec3Ptr => $"new FcList<{nameof(float3)}>.Ref(null, 0)",
            SignalType.Rot => $"{nameof(Quaternion)}.{nameof(Quaternion.Identity)}",
            SignalType.RotPtr => $"new FcList<{nameof(Quaternion)}>.Ref(null, 0)",
            SignalType.Bool => "false",
            SignalType.BoolPtr => "new FcList<bool>.Ref(null, 0)",
            SignalType.Obj => $"{nameof(FcObject)}.{nameof(FcObject.Null)}",
            SignalType.ObjPtr => $"new FcList<{nameof(FcObject)}>.Ref(null, 0)",
            SignalType.Con => $"{nameof(FcConstraint)}.{nameof(FcConstraint.Null)}",
            SignalType.ConPtr => $"new FcList<{nameof(FcConstraint)}>.Ref(null, 0)",
            _ => throw new UnreachableException(),
        };

    private static string GetEntryPointMethodName(EntryPoint entryPoint, bool ptr)
        => $"Run{entryPoint.EnvironmentIndex}{(ptr ? "_ptr" : string.Empty)}_{entryPoint.BlockPos.X}_{entryPoint.BlockPos.Y}_{entryPoint.BlockPos.Z}__{entryPoint.TerminalPos.X}_{entryPoint.TerminalPos.Y}_{entryPoint.TerminalPos.Z}";

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
