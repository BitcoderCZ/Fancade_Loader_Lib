// <copyright file="FcAstCompiler.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Fancade.Editing.Scripting.Settings;
using BitcoderCZ.Fancade.Runtime.Compiled.Exceptions;
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
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
#if NETCOREAPP
using System.Runtime.Loader;
#endif
using System.Text;
using static BitcoderCZ.Fancade.Utils.ThrowHelper;

namespace BitcoderCZ.Fancade.Runtime.Compiled;

/// <summary>
/// Transpile <see cref="FcAST"/> into C#.
/// </summary>
public sealed partial class FcAstCompiler
{
    private readonly FcEnvironment[] _environments;
    private readonly StringBuilder _writerBuilder;
    private readonly IndentedTextWriter _writer;

    private readonly StatementExecutionMode _executionMode;
    private readonly TimeSpan _timeout;

    private readonly Dictionary<(int, Variable), string> _varToName = [];

    private readonly ImmutableArray<(int, Variable)> _variables;

    private readonly Queue<(SyntaxTerminal Terminal, int EnvironmentIndex, SignalType Type)> _nodesToWrite = [];
    private readonly HashSet<(EntryPoint EntryPoint, bool IsPtr)> _writtenNodes = [];

    private readonly Dictionary<(EntryPoint EntryPoint, bool IsPtr), int>? _terminalToIndex;

    private readonly HashSet<(string Name, string Type, string? DefaultValue)> _stateStoreVariables = [];

    private int _localVarCounter = 0;

    private FcAstCompiler(FcAST ast, Options options)
    {
        _executionMode = options.StatementExecutionMode;
        _timeout = options.Timeout;

        if (_executionMode is StatementExecutionMode.StateMachine)
        {
            _terminalToIndex = [];
        }

        List<FcEnvironment> environments = [];
        List<ImmutableArray<Variable>> variables = [];

        var mainEnvironment = new FcEnvironment(ast, 0, -1, int3.Zero);
        environments.Add(mainEnvironment);
        variables.Add(mainEnvironment.AST.Variables);

        InitEnvironments(mainEnvironment, environments, variables, options.MaxDepth);

        _environments = [.. environments];
        _variables = [.. variables.Select((var, index) => (index, var)).SelectMany(item => item.var.Select(var => (item.index, var)))];

        _writerBuilder = new StringBuilder();
        _writer = new IndentedTextWriter(new StringWriter(_writerBuilder), options.HumanReadable ? IndentedTextWriter.DefaultTabString : string.Empty);
        if (!options.HumanReadable)
        {
            _writer.NewLine = string.Empty;
        }
    }

    /// <summary>
    /// Specifies how statements are emitted.
    /// </summary>
    public enum StatementExecutionMode
    {
        /// <summary>
        /// Emits statements as a state machine.
        /// </summary>
        StateMachine = 0,

        /// <summary>
        /// Emits statements as function calls, may be faster than <see cref="StateMachine"/>, but can lead to <see cref="StackOverflowException"/>.
        /// </summary>
        DirectCalls,
    }

    /// <summary>
    /// Transpiles a <see cref="FcAST"/> into C# and compiles it into an <see cref="IAstRunner"/>.
    /// </summary>
    /// <param name="ast">The <see cref="FcAST"/> to compile.</param>
    /// <param name="ctx">The <see cref="IRuntimeContext"/> to use.</param>
    /// <param name="options">The <see cref="Options"/> to use.</param>
    /// <returns>The compiled <see cref="IAstRunner"/>.</returns>
    /// <exception cref="CompilationErrorException">Thrown when the transpiled code contains errors.</exception>
    public static IAstRunner Compile(FcAST ast, IRuntimeContext ctx, Options options)
        => TryCompile(ast, ctx, options, out _, out var runner, out var diagnostics)
            ? runner
            : throw new CompilationErrorException(diagnostics);

    /// <summary>
    /// Transpiles a <see cref="FcAST"/> into C# and compiles it into an <see cref="IAstRunner"/>.
    /// </summary>
    /// <param name="ast">The <see cref="FcAST"/> to compile.</param>
    /// <param name="ctx">The <see cref="IRuntimeContext"/> to use.</param>
    /// <param name="options">The <see cref="Options"/> to use.</param>
    /// <param name="code">The transpiled code.</param>
    /// <param name="runner">The compiled <see cref="IAstRunner"/>.</param>
    /// <param name="diagnostics">The errors in <paramref name="code"/>.</param>
    /// <returns><see langword="true"/> if the transpiled code was successfully compiled; otherwise, <see langword="false"/>.</returns>
    public static bool TryCompile(FcAST ast, IRuntimeContext ctx, Options options, out string code, [NotNullWhen(true)] out IAstRunner? runner, [NotNullWhen(false)] out IEnumerable<Diagnostic>? diagnostics)
    {
        ThrowIfNull(ast);
        ThrowIfNull(ctx);
        ThrowIfNull(options);

        var compiler = new FcAstCompiler(ast, options);

        code = compiler.WriteAll();

        return TryCompileInternal(code, ctx, options.AdditionalReferences, options.LoadAssemblyFunc, out diagnostics, out runner);
    }

    private static bool TryCompileInternal(string code, IRuntimeContext ctx, IEnumerable<MetadataReference> additionalReferences, Func<MemoryStream, Assembly> loadAssemblyFunc, [NotNullWhen(false)] out IEnumerable<Diagnostic>? diagnostics, [NotNullWhen(true)] out IAstRunner? runner)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code, new CSharpParseOptions(languageVersion: LanguageVersion.CSharp13));

        string assemblyName = Path.GetRandomFileName();
        IEnumerable<MetadataReference> references =
        [
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Stack<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ValueType).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
            MetadataReference.CreateFromFile(typeof(IRuntimeContext).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(int3).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Vector3).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Diagnostics.Stopwatch).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(SignalType).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Ranking).Assembly.Location),
            .. additionalReferences,
        ];

        try
        {
            // does not work in unity
            var vectorsRef = MetadataReference.CreateFromFile(Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "System.Numerics.Vectors.dll"));
            references = references.Append(vectorsRef);
        }
        catch
        {
        }

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
                diagnostics = result.Diagnostics.Where(diagnostic =>
                   diagnostic.IsWarningAsError ||
                   diagnostic.Severity == DiagnosticSeverity.Error);

                runner = null;

                return false;
            }
            else
            {
                ms.Seek(0, SeekOrigin.Begin);
                Assembly assembly = loadAssemblyFunc(ms);

                Type type = assembly.GetType("BitcoderCZ.Fancade.Runtime.Compiled.Generated.CompiledAST`1")!.MakeGenericType([ctx.GetType()]);
                object obj = Activator.CreateInstance(type, [ctx])!;

                diagnostics = null;
                runner = (IAstRunner)obj;
                return true;
            }
        }
    }

    private static void InitEnvironments(FcEnvironment outer, List<FcEnvironment> environments, List<ImmutableArray<Variable>> variables, int maxDepth, int depth = 1)
    {
        if (depth > maxDepth)
        {
            throw new EnvironmentDepthLimitReachedException();
        }

        foreach (var statement in outer.AST.Statements.Values)
        {
            if (statement is CustomStatementSyntax customStatement)
            {
                var environment = new FcEnvironment(customStatement.AST, environments.Count, outer.Index, customStatement.Position);
                environments.Add(environment);
                variables.Add(environment.AST.Variables);
                outer.BlockData[customStatement.Position] = environment;

                InitEnvironments(environment, environments, variables, maxDepth, depth + 1);
            }
        }
    }

    private static void WriteEnvironmentPosition(int environmentIndex, int3 blockPos, IndentedTextWriter writer)
        => writer.WriteInv($"new EnvironmentPosition(_environments[{environmentIndex}], new int3({blockPos.X}, {blockPos.Y}, {blockPos.Z}))");

    private static void WriteScreenInfo(IndentedTextWriter writer)
        => writer.Write($"new global::BitcoderCZ.Fancade.Runtime.Utils.ScreenInfo(_ctx.{nameof(IRuntimeContextBase.ScreenSize)})");

    private static string GetStateStoreVarName(int environmentIndex, int3 blockPos, string suffix)
        => $"store_{environmentIndex}_{blockPos.X}_{blockPos.Y}_{blockPos.Z}_{suffix}";

    private static string GetCSharpName(SignalType type)
        => type switch
        {
            SignalType.Void => "void",
            SignalType.Float => "float",
            SignalType.FloatPtr => "FcList<float>.Ref",
            SignalType.Vec3 => nameof(Vector3),
            SignalType.Vec3Ptr => $"FcList<{nameof(Vector3)}>.Ref",
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
            SignalType.Vec3 => $"{nameof(Vector3)}.{nameof(Vector3.Zero)}",
            SignalType.Vec3Ptr => $"new FcList<{nameof(Vector3)}>.Ref(null, 0)",
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

    private static int IntLength(int i)
        => i switch
        {
            < 0 => (int)Math.Floor(Math.Log10(-i)) + 2,
            0 => 1,
            _ => (int)Math.Floor(Math.Log10(i)) + 1,
        };

    private string WriteAll()
    {
        _writer.WriteLine("""
            using BitcoderCZ.Fancade.Editing;
            using BitcoderCZ.Fancade.Editing.Scripting.Settings;
            using BitcoderCZ.Fancade.Runtime;
            using BitcoderCZ.Fancade.Runtime.Exceptions;
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
                
                private readonly FcRandom _rng = new FcRandom();

                private Queue<Action>? lateUpdateQueue = new();

                private static readonly CompFcEnvironment[] _environments = new CompFcEnvironment[]
                {
                """);

            _writer.Indent++;
            foreach (var env in _environments)
            {
                _writer.WriteLineInv($"new CompFcEnvironment({env.PrefabId}, {env.Index}, {env.OuterEnvironmentIndex}, new {nameof(int3)}({env.OuterPosition.X}, {env.OuterPosition.Y}, {env.OuterPosition.Z}), {(env.IsObject ? "true" : "false")}),");
            }

            _writer.Indent--;
            _writer.WriteLine("};");
            _writer.WriteLine();

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
                    private readonly FcList<{GetCSharpName(variable.Type.ToNotPointer())}> {GetVariableName(environmentIndex, variable)} = new("{variable.Name}", {nameof(SignalType)}.{variable.Type});
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

            _writer.WriteLineAll("""
                ];
                            
                public int EnvironmentCount => _environments.Length;

                """);

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

                foreach (var entryPoint in FcEnvironment.GetEntryPointsInExecutionOrder(_environments))
                {
                    switch (_executionMode)
                    {
                        case StatementExecutionMode.StateMachine:
                            // TODO: store entry point indexes in an array and loop over them? (in generated code)
                            _writer.WriteLineInv($"""
                                Run({GetTerminalIndex(entryPoint, false)});
                                """);
                            _nodesToWrite.Enqueue((new SyntaxTerminal(_environments[entryPoint.EnvironmentIndex].AST.Statements[entryPoint.BlockPos], entryPoint.TerminalPos), entryPoint.EnvironmentIndex, SignalType.Void));
                            break;
                        case StatementExecutionMode.DirectCalls:
                            WriteDirectEntryPoint(entryPoint, false, _writer);
                            break;
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

            using (_writer.CurlyIndent("public IFcEnvironment GetEnvironment(int index)"))
            {
                _writer.WriteLine("return _environments[index];");
            }

            using (_writer.CurlyIndent("public void Dispose()"))
            {
                // empty
            }

            if (_timeout != Timeout.InfiniteTimeSpan)
            {
                _writer.WriteLineAll("""
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    private void ThrowIfTimeout(EnvironmentPosition currentBlock)
                    {
                        if (_timeoutWatch.Elapsed > _timeout)
                        {
                            ThrowFcTimeoutException(currentBlock);
                        }
                    }

                    private static void ThrowFcTimeoutException(EnvironmentPosition currentBlock)
                    {
                        throw new FcTimeoutException(currentBlock);
                    }

                    """);
            }

            var nonVoidNodes = new Stack<(SyntaxTerminal Terminal, int EnvironmentIndex, SignalType Type)>();
            if (_executionMode is StatementExecutionMode.StateMachine)
            {
                using (_writer.CurlyIndent("private void Run(int entryTerminal)"))
                {
                    // TODO: pool stacks
                    _writer.WriteLineAll("""
                        Stack<int> returnStack = new(1024);

                        returnStack.Push(entryTerminal);

                        while (returnStack.TryPop(out var terminal))
                        {
                        """);
                    _writer.Indent++;

                    if (_timeout != Timeout.InfiniteTimeSpan)
                    {
                        _writer.WriteLine("""
                            ThrowIfTimeout(_indexToEnvironmentPosition[terminal]);

                            """);
                    }

                    _writer.WriteLineAll("""
                        switch (terminal)
                        {
                        """);
                    _writer.Indent++;
                    while (_nodesToWrite.TryDequeue(out var item))
                    {
                        if (item.Type != SignalType.Void)
                        {
                            nonVoidNodes.Push(item);
                            continue;
                        }

                        var (terminal, environmentIndex, type) = item;

                        var entryPoint = new EntryPoint(environmentIndex, terminal.Node.Position, terminal.Position);

                        if (!_writtenNodes.Add((entryPoint, type.IsPointer())))
                        {
                            continue;
                        }

                        _writer.WriteLineInv($"""
                            case {GetTerminalIndex(entryPoint, type is not SignalType.Void && type.IsPointer())}:
                            """);
                        _writer.Indent++;

                        // TODO: don't write the case if no statements gets written and there are 0 connected, or if there is 1 connected, rewrite all references to it, somehow
                        var environment = _environments[environmentIndex];
                        var statement = WriteStatement(entryPoint.BlockPos, entryPoint.TerminalPos, environment, out var executeNext, _writer);

                        if (executeNext != new byte3(255, 255, 255))
                        {
                            WritePushStackConnected(statement, executeNext, environment, _writer);
                        }

                        _writer.WriteLine("break;");
                        _writer.Indent--;
                    }

                    _writer.Indent -= 2;
                    _writer.WriteLineAll("""
                            }
                        }
                        """);
                }

                Debug.Assert(_terminalToIndex is not null, $"{nameof(_terminalToIndex)} should not be null.");
                _writer.WriteLineAllInv($"""
                    private static readonly {nameof(EnvironmentPosition)}[] _indexToEnvironmentPosition = 
                    [
                    """);
                _writer.Indent++;

                int nextExpectedIndex = 0;
                foreach (var (terminal, terminalIndex) in _terminalToIndex.OrderBy(item => item.Value))
                {
                    if (nextExpectedIndex < terminalIndex)
                    {
                        for (int i = 0; i < (terminalIndex - nextExpectedIndex); i++)
                        {
                            _writer.WriteLine("default,");
                        }
                    }

                    WriteEnvironmentPosition(terminal.EntryPoint.EnvironmentIndex, terminal.EntryPoint.BlockPos, _writer);
                    _writer.WriteLine(',');

                    nextExpectedIndex = terminalIndex + 1;
                }

                _writer.Indent--;
                _writer.WriteLineAll("""
                    ];

                    """);

                while (nonVoidNodes.TryPop(out var item))
                {
                    _nodesToWrite.Enqueue(item);
                }
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
                        _writer.Write("ThrowIfTimeout(");
                        WriteEnvironmentPosition(entryPoint.EnvironmentIndex, entryPoint.BlockPos, _writer);
                        _writer.WriteLine("""
                            );

                            """);
                    }

                    if (type == SignalType.Void)
                    {
                        Debug.Assert(_executionMode is not StatementExecutionMode.StateMachine, $"If {nameof(_executionMode)} is {nameof(StatementExecutionMode.StateMachine)}, all void nodes should have already been written, and expressions should not create statements.");
                        WriteDirectEntryPoint(entryPoint, true, _writer);
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

            using (_writer.CurlyIndent("public void Reset()"))
            {
                if (_timeout != Timeout.InfiniteTimeSpan)
                {
                    _writer.WriteLine("_timeoutWatch.Reset();");
                }

                foreach (var (environmentIndex, variable) in _environments[0].AST.GlobalVariables.Select(var => (-1, var)).Concat(_variables))
                {
                    _writer.WriteLineInv($"{GetVariableName(environmentIndex, variable)}.Clear();");
                }

                foreach (var (varName, _, _) in _stateStoreVariables)
                {
                    _writer.WriteLineInv($"{varName} = default;");
                }
            }
        }

        _writer.WriteLine("""
            public sealed class FcList<T> where T : struct
            {
                private static readonly T DefaultValue = typeof(T) == typeof(Quaternion) ? (T)(object)Quaternion.Identity : default;

                private readonly string _name;
                private readonly SignalType _type;

                private T[] _items;
                private int _count;

                public FcList(string name, SignalType type)
                {
                    _name = name;
                    _type = type;
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

                public void Clear()
                    => _count = 0;

                public Variable ToVariable()
                    => new Variable(_name, _type);

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
                    else if (typeof(T) == typeof(Vector3))
                    {
                        for (int i = 0; i < _count; i++)
                        {
                            result[i] = new RuntimeValue((Vector3)(object)_items[i]);
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
            
                    public Variable ToVariable()
                        => _list is null ? default : _list.ToVariable();
                }
            }

            internal sealed class CompFcEnvironment : IFcEnvironment
            {
                public CompFcEnvironment(ushort prefabId, int index, int outerEnvironmentIndex, int3 outerPosition, bool isObject)
                {
                    PrefabId = prefabId;
                    Index = index;
                    OuterEnvironmentIndex = outerEnvironmentIndex;
                    OuterPosition = outerPosition;
                    IsObject = isObject;
                }
                public ushort PrefabId { get; }
                public int Index { get; }
                public int OuterEnvironmentIndex { get; }
                public int3 OuterPosition { get; }
                public bool IsObject { get; }
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

                public static Quaternion ToQuatDeg(this Vector3 value)
                    => Quaternion.CreateFromYawPitchRoll(value.Y * DegToRad, value.X * DegToRad, value.Z * DegToRad);

                public static Vector3 LineVsPlane(Vector3 lineFrom, Vector3 lineTo, Vector3 planePoint, Vector3 planeNormal)
                {
                    float t = Vector3.Dot(planePoint - lineFrom, planeNormal) / Vector3.Dot(lineTo - lineFrom, planeNormal);
                    return (lineFrom + (t * (lineTo - lineFrom)));
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

    private void WriteDirectEntryPoint(EntryPoint entryPoint, bool direct, IndentedTextWriter writer)
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

            VisitConnected(statement, executeNext, environment, queue.Enqueue, reverse: false);

            direct = false;
        }
    }

    private void WritePushStackEntryPoint(EntryPoint entryPoint, IndentedTextWriter writer)
    {
        writer.WriteLineInv($"""
            returnStack.Push({GetTerminalIndex(entryPoint, false)});
            """);
        _nodesToWrite.Enqueue((new SyntaxTerminal(_environments[entryPoint.EnvironmentIndex].AST.Statements[entryPoint.BlockPos], entryPoint.TerminalPos), entryPoint.EnvironmentIndex, SignalType.Void));
    }

    private void WriteRunEntryPoint(EntryPoint entryPoint, IndentedTextWriter writer)
    {
        writer.WriteLineInv($"""
            Run({GetTerminalIndex(entryPoint, false)});
            """);
        _nodesToWrite.Enqueue((new SyntaxTerminal(_environments[entryPoint.EnvironmentIndex].AST.Statements[entryPoint.BlockPos], entryPoint.TerminalPos), entryPoint.EnvironmentIndex, SignalType.Void));
    }

    private void WriteDirectConnected(StatementSyntax statement, byte3 terminalPos, FcEnvironment environment, IndentedTextWriter writer)
        => VisitConnected(statement, terminalPos, environment, entryPont => WriteDirectEntryPoint(entryPont, false, writer), reverse: false);

    private void WritePushStackConnected(StatementSyntax statement, byte3 terminalPos, FcEnvironment environment, IndentedTextWriter writer)
        => VisitConnected(
            statement,
            terminalPos,
            environment,
            connectedEntryPoint =>
            {
                writer.WriteLineInv($"""
                    returnStack.Push({GetTerminalIndex(connectedEntryPoint, false)});
                    """);
                _nodesToWrite.Enqueue((new SyntaxTerminal(_environments[connectedEntryPoint.EnvironmentIndex].AST.Statements[connectedEntryPoint.BlockPos], connectedEntryPoint.TerminalPos), connectedEntryPoint.EnvironmentIndex, SignalType.Void));
            },
            reverse: true);

    private void WriteRunConnected(StatementSyntax statement, byte3 terminalPos, FcEnvironment environment, IndentedTextWriter writer)
        => VisitConnected(
            statement,
            terminalPos,
            environment,
            connectedEntryPoint =>
            {
                writer.WriteLineInv($"""
                    Run({GetTerminalIndex(connectedEntryPoint, false)});
                    """);
                _nodesToWrite.Enqueue((new SyntaxTerminal(_environments[connectedEntryPoint.EnvironmentIndex].AST.Statements[connectedEntryPoint.BlockPos], connectedEntryPoint.TerminalPos), connectedEntryPoint.EnvironmentIndex, SignalType.Void));
            },
            reverse: false);

    private void VisitConnected(StatementSyntax statement, byte3 terminalPos, FcEnvironment environment, Action<EntryPoint> action, bool reverse)
    {
        var connections = statement.OutVoidConnections;

        foreach (var connection in reverse
            ? connections.Reverse()
            : connections)
        {
            if (connection.FromVoxel == terminalPos)
            {
                if (connection.IsToOutside)
                {
                    var outerEnvironment = _environments[environment.OuterEnvironmentIndex];

                    VisitConnected(outerEnvironment.AST.Statements[environment.OuterPosition], connection.ToVoxel, outerEnvironment, action, reverse);
                }
                else
                {
                    action(new(environment.Index, connection.To, connection.ToVoxel));
                }
            }
        }
    }

    private bool TryWriteDirectRef(SyntaxTerminal terminal, FcEnvironment environment, IndentedTextWriter writer)
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
                        return TryWriteDirectRef(list.Variable, environment, writer);
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

                Debug.Assert(written, $"Writing {nameof(environmentIndex)} into {nameof(span)} should always succeed.");

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

    private int GetTerminalIndex(EntryPoint entryPoint, bool ptr)
    {
        Debug.Assert(_terminalToIndex is not null, $"{nameof(_terminalToIndex)} should not be null.");

        if (_terminalToIndex.TryGetValue((entryPoint, ptr), out int index))
        {
            return index;
        }
        else
        {
            index = _terminalToIndex.Count;
            _terminalToIndex.Add((entryPoint, ptr), index);
            return index;
        }
    }

    private readonly struct ExpressionInfo
    {
        public readonly SignalType Type;
        public readonly string? VariableName;

        public ExpressionInfo(SignalType type)
        {
            Type = type;
        }

        public ExpressionInfo(Variable variable)
        {
            Type = variable.Type.ToPointer();
            VariableName = variable.Name;
        }
    }

    /// <summary>
    /// Options for <see cref="FcAstCompiler"/>.
    /// </summary>
    public sealed class Options
    {
        /*/// <summary>
        /// Default <see cref="Options"/>.
        /// </summary>
        public static readonly Options Default =
#if NETCOREAPP
            new Options(AssemblyLoadContext.Default);
#else
            new Options(AppDomain.CurrentDomain);
#endif*/

        private readonly Func<MemoryStream, Assembly> _loadAssemblyFunc = null!;
        private readonly IEnumerable<MetadataReference> _additionalReferences = [];

        private readonly TimeSpan _timeout = TimeSpan.FromSeconds(3);
        private readonly int _maxDepth = 4;

        /// <summary>
        /// Initializes a new instance of the <see cref="Options"/> class.
        /// </summary>
        /// <param name="appDomain">An <see cref="AppDomain"/> to load the assembly into.</param>
        public Options(AppDomain appDomain)
        {
            _loadAssemblyFunc = stream => appDomain.Load(stream.ToArray());
        }

#if NETCOREAPP
        /// <summary>
        /// Initializes a new instance of the <see cref="Options"/> class.
        /// </summary>
        /// <param name="assemblyLoadContext">An <see cref="AssemblyLoadContext"/> to load the assembly into.</param>
        public Options(AssemblyLoadContext assemblyLoadContext)
        {
            _loadAssemblyFunc = assemblyLoadContext.LoadFromStream;
        }
#endif

        /// <summary>
        /// Initializes a new instance of the <see cref="Options"/> class.
        /// </summary>
        /// <param name="loadAssemblyFunc">Function used to load the assembly.</param>
        public Options(Func<MemoryStream, Assembly> loadAssemblyFunc)
        {
            _loadAssemblyFunc = loadAssemblyFunc;
        }

        /// <summary>
        /// Gets the function used to load the assembly.
        /// </summary>
        /// <value>Function used to load the assembly.</value>
        public Func<MemoryStream, Assembly> LoadAssemblyFunc => _loadAssemblyFunc;

        /// <summary>
        /// Gets additional <see cref="MetadataReference"/>s used when compiling the transpiled code.
        /// </summary>
        /// <value>Additional <see cref="MetadataReference"/>s used when compiling the transpiled code.</value>
        public IEnumerable<MetadataReference> AdditionalReferences
        {
            get => _additionalReferences;
            init
            {
                ThrowIfNull(value);
                _additionalReferences = value;
            }
        }

        /// <summary>
        /// Gets the statement execution mode, <see cref="StatementExecutionMode.StateMachine"/> by default.
        /// </summary>
        /// <value>Statement execution mode.</value>
        public StatementExecutionMode StatementExecutionMode
        {
            get;
            init;
        }

        /// <summary>
        /// Gets a value indicating whether the transpiled code should be human readable, <see langword="false"/> by default.
        /// </summary>
        /// <value><see langword="true"/> if the transpiled code should be human readable; otherwise, <see langword="false"/>.</value>
        public bool HumanReadable { get; init; } // TODO: don't indent and write new lines if false

        /// <summary>
        /// Gets the time after which <see cref="FcTimeoutException"/> will be thrown, 3s by default.
        /// </summary>
        /// <value>Time after which <see cref="FcTimeoutException"/> will be thrown.</value>
        public TimeSpan Timeout
        {
            get => _timeout;
            init
            {
                if (value.Ticks <= 0 && value != System.Threading.Timeout.InfiniteTimeSpan)
                {
                    ThrowArgumentOutOfRangeException(nameof(value), $"{nameof(Timeout)} must be greater than 0, or equal to {nameof(System.Threading.Timeout)}.{nameof(System.Threading.Timeout.InfiniteTimeSpan)}.");
                }

                _timeout = value;
            }
        }

        /// <summary>
        /// Gets the maximum environment depth (blocks inside blocks), 4 by default.
        /// </summary>
        /// <value>The maximum environment depth (blocks inside blocks).</value>
        public int MaxDepth
        {
            get => _maxDepth;
            init
            {
                ThrowIfLessThan(value, 1);

                _maxDepth = value;
            }
        }
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
