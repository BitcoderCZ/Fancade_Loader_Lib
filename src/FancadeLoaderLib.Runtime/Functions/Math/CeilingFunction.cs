using FancadeLoaderLib.Editing;
using MathUtils.Vectors;
using System.Diagnostics;

namespace FancadeLoaderLib.Runtime.Functions.Math;

public sealed class CeilingFunction : UnaryFunction
{
    public CeilingFunction(RuntimeTerminal input)
        : base(input)
    {
    }

    public override TerminalOutput GetTerminalOutput(byte3 terminalPos, IRuntimeContext context)
    {
        Debug.Assert(terminalPos == TerminalDef.GetOutPosition(0, 2, 1), $"{nameof(terminalPos)} should be valid.");

        return new TerminalOutput(new RuntimeValue(MathF.Ceiling(Input.GetOutput(context).GetValue(context).Float)));
    }
}
