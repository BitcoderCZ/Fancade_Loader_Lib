using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Runtime.Exceptions;
using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace FancadeLoaderLib.Runtime.Functions.Control;

public sealed class LateUpdateFunction : IActiveFunction
{
    public static readonly byte3 AfterPhysicsPos = TerminalDef.GetOutPosition(0, 2, 2);

    public int Execute(byte3 terminalPos, IRuntimeContext context, Span<byte3> executeNext)
    {
        Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be the before terminal.");

        // after physics executed by the interpreter
        executeNext[0] = TerminalDef.AfterPosition;

        return 1;
    }

    public TerminalOutput GetTerminalOutput(byte3 terminalPos, IRuntimeContext context)
        => throw new InvalidTerminalException(terminalPos);
}
