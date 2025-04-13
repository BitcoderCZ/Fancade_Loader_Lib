using FancadeLoaderLib.Editing;
using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace FancadeLoaderLib.Runtime.Functions.Game;

public sealed class CurrentFrameFunction : IFunction
{
    public TerminalOutput GetTerminalOutput(byte3 terminalPos, IRuntimeContext context)
    {
        Debug.Assert(terminalPos == TerminalDef.GetOutPosition(0, 2, 1), $"{nameof(terminalPos)} should be valid.");

        return new TerminalOutput(new RuntimeValue((float)context.CurrentFrame));
    }
}
