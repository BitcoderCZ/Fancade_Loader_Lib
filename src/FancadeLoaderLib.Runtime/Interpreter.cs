using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Runtime.Syntax;
using FancadeLoaderLib.Runtime.Syntax.Values;
using FancadeLoaderLib.Runtime.Syntax.Variables;
using MathUtils.Vectors;
using System.Collections.Frozen;
using System.Diagnostics;
using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib.Runtime;

public sealed class Interpreter
{
    private readonly AST _ast;
    private readonly IRuntimeContext _ctx;

    private readonly VariableAccessor _variableAccessor;

    public Interpreter(AST ast, IRuntimeContext ctx)
    {
        ThrowIfNull(ast, nameof(ast));
        ThrowIfNull(ast, nameof(ctx));

        _ast = ast;
        _ctx = ctx;

        _variableAccessor = new VariableAccessor(_ast.GlobalVariables.Concat(_ast.Variables[ast.PrefabId]));
    }

    public Action RunFrame()
    {
        Queue<(ushort3 BlockPosition, byte3 TerminalPos)> lateUpdateQueue = new Queue<(ushort3 BlockPosition, byte3 TerminalPos)>();

        foreach (var entryPoint in _ast.EntryPoints)
        {
            Execute(entryPoint, lateUpdateQueue);
        }

        return () =>
        {
            while (lateUpdateQueue.TryDequeue(out var entryPoint))
            {
                Execute(entryPoint, null);
            }
        };
    }

    private void Execute((ushort3 BlockPosition, byte3 TerminalPos) entryPoint, Queue<(ushort3 BlockPosition, byte3 TerminalPos)>? lateUpdateQueue)
    {
        Span<byte3> executeNextSpan = stackalloc byte3[16];

        Stack<(ushort3 BlockPosition, byte3 TerminalPos)> executeNext = new();

        executeNext.Push(entryPoint);

        while (executeNext.TryPop(out var item))
        {
            var (blockPos, terminalPos) = item;
            var statement = (StatementSyntax)_ast.Nodes[blockPos];

            int nextCount = 0;
            executeNextSpan[nextCount++] = TerminalDef.AfterPosition;

            // faster than switching on type
            switch (statement.PrefabId)
            {
                // ******************** Value ********************
                case 16 or 20 or 24 or 28 or 32:
                    {
                        var inspect = (InspectStatementSyntax)statement;
                        if (inspect.Input is not null)
                        {
                            var output = GetTerminalOutput(inspect.Input);

                            _ctx.InspectValue(output.GetValue(_variableAccessor), inspect.Type, _ast.PrefabId, inspect.Position);
                        }
                    }

                    break;

                case 428 or 430 or 432 or 434 or 436 or 438:
                    {
                        var setVar = (SetVaribleStatementSyntax)statement;

                        if (setVar.Value is not null)
                        {
                            var value = GetTerminalOutput(setVar.Value).GetValue(_variableAccessor);
                            _variableAccessor.SetVariableValue(_variableAccessor.GetVariableId(setVar.Variable), 0, value);
                        }
                    }

                    break;
                default:
                    throw new NotImplementedException($"Prefab with id {statement.PrefabId} is not yet implemented.");
            }

            foreach (var nextTerminal in executeNextSpan[..nextCount])
            {
                foreach (var connection in statement.OutVoidConnections)
                {
                    if (connection.FromVoxel == nextTerminal)
                    {
                        executeNext.Push((connection.To, (byte3)connection.ToVoxel));
                    }
                }
            }
        }
    }

    private TerminalOutput GetTerminalOutput(SyntaxTerminal terminal)
    {
        // faster than switching on type
        switch (terminal.Node.PrefabId)
        {
            case 36 or 38 or 42 or 449 or 451:
                {
                    Debug.Assert(terminal.TerminalPosition == TerminalDef.GetOutPosition(0, 2, terminal.Node.PrefabId is 38 or 42 ? 2 : 1), $"{nameof(terminal)}.{nameof(terminal.TerminalPosition)} should be valid.");
                    var literal = (LiteralExpressionSyntax)terminal.Node;

                    return new TerminalOutput(literal.Value);
                }

            case 46 or 48 or 50 or 52 or 54 or 56:
                {
                    Debug.Assert(terminal.TerminalPosition == TerminalDef.GetOutPosition(0, 2, 1), $"{nameof(terminal)}.{nameof(terminal.TerminalPosition)} should be valid.");
                    var getVar = (GetVariableExpressionSyntax)terminal.Node;

                    return new TerminalOutput(new VariableReference(_variableAccessor.GetVariableId(getVar.Variable), 0));
                }

            default:
                throw new NotImplementedException($"Prefab with id {terminal.Node.PrefabId} is not yet implemented.");
        }
    }

    private sealed class VariableAccessor : IVariableAccessor
    {
        private readonly FrozenDictionary<Variable, int> _variableToId;
        private readonly VariableManager _variableManager;

        public VariableAccessor(IEnumerable<Variable> variables)
        {
            int varId = 0;
            _variableToId = variables.ToFrozenDictionary(var => var, _ => varId++);
            _variableManager = new VariableManager(_variableToId.Count);
        }

        public int GetVariableId(Variable variable)
            => _variableToId[variable];

        public RuntimeValue GetVariableValue(int variableId, int index)
            => _variableManager.GetVariableValue(variableId, index);

        public void SetVariableValue(int variableId, int index, RuntimeValue value)
            => _variableManager.SetVariableValue(variableId, index, value);
    }
}
