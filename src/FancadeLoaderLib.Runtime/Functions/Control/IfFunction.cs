using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Runtime.Exceptions;
using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace FancadeLoaderLib.Runtime.Functions.Control;

public sealed class IfFunction : IActiveFunction
{
    private static readonly byte3 TruePos = TerminalDef.GetOutPosition(0, 2, 2);
    private static readonly byte3 FalsePos = TerminalDef.GetOutPosition(1, 2, 2);

    private readonly RuntimeTerminal _condition;

    public IfFunction(RuntimeTerminal condition)
    {
        _condition = condition;
    }

    public int Execute(byte3 terminalPos, IRuntimeContext context, Span<byte3> executeNext)
    {
        Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be the before terminal.");

        var output = _condition.GetOutput(context);

        int exeCount = 0;

        executeNext[exeCount++] = TerminalDef.AfterPosition;

        if (output.IsConnected)
        {
            if (output.GetValue(context).Bool)
            {
                executeNext[exeCount++] = TruePos;
            }
            else
            {
                executeNext[exeCount++] = FalsePos;
            }
        }

        return exeCount;
    }

    public TerminalOutput GetTerminalOutput(byte3 terminalPos, IRuntimeContext context)
        => throw new InvalidTerminalException(terminalPos);
}
