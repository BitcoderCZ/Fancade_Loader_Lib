using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Text;
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

    public void RunFrame()
    {
        Span<byte3> executeNextSpan = stackalloc byte3[16];

        Stack<(ushort3 BlockPosition, byte3 TerminalPos)> executeNext = new();

        foreach (var entryPoint in _ast.EntryPoints)
        {
            executeNext.Push(entryPoint);

            while (executeNext.TryPop(out var item))
            {
                var (blockPos, terminalPos) = item;
                var ins = _ast.Functions[blockPos];

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
}
