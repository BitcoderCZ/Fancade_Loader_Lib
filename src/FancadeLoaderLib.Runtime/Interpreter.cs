using FancadeLoaderLib.Runtime.Functions.Control;
using MathUtils.Vectors;
using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib.Runtime;

public sealed class Interpreter
{
    private readonly AST _ast;

    public Interpreter(AST ast)
    {
        ThrowIfNull(ast, nameof(ast));

        _ast = ast;
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
            var ins = _ast.Functions[blockPos];

            if (ins.Function is LateUpdateFunction && lateUpdateQueue is not null)
            {
                foreach (var connection in ins.Connections)
                {
                    if (connection.FromVoxel == LateUpdateFunction.AfterPhysicsPos)
                    {
                        lateUpdateQueue.Enqueue((connection.To, (byte3)connection.ToVoxel));
                    }
                }
            }

            int nextCount = ((IActiveFunction)ins.Function).Execute(terminalPos, _ast.RuntimeContext, executeNextSpan);

            foreach (var nextTerminal in executeNextSpan[..nextCount])
            {
                foreach (var connection in ins.Connections)
                {
                    if (connection.FromVoxel == nextTerminal)
                    {
                        executeNext.Push((connection.To, (byte3)connection.ToVoxel));
                    }
                }
            }
        }
    }
}
