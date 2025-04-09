using FancadeLoaderLib.Editing;
using MathUtils.Vectors;
using System.Diagnostics;

namespace FancadeLoaderLib.Runtime.Functions.Math;

public sealed class ScaleFunction : BinaryFunction
{
    public ScaleFunction(RuntimeTerminal input1, RuntimeTerminal input2)
        : base(input1, input2)
    {
    }

    public override TerminalOutput GetTerminalOutput(byte3 terminalPos, IRuntimeContext context)
    {
        Debug.Assert(terminalPos == TerminalDef.GetOutPosition(0, 2, 2), $"{nameof(terminalPos)} should be valid.");
        return new TerminalOutput(new RuntimeValue(Input1.GetOutput(context).GetValue(context).Float3 * Input2.GetOutput(context).GetValue(context).Float));
    }
}
