using FancadeLoaderLib.Editing;
using MathUtils.Vectors;
using System.Diagnostics;

namespace FancadeLoaderLib.Runtime.Functions.Math;

public sealed class EqualsNumbersFunction : BinaryFunction
{
    public EqualsNumbersFunction(RuntimeTerminal input1, RuntimeTerminal input2)
        : base(input1, input2)
    {
    }

    public override TerminalOutput GetTerminalOutput(byte3 terminalPos, IRuntimeContext context)
    {
        Debug.Assert(terminalPos == TerminalDef.GetOutPosition(0, 2, 2), $"{nameof(terminalPos)} should be valid.");

        const float MaxDiff = 0.001f;

        return new TerminalOutput(new RuntimeValue(MathF.Abs(Input1.GetOutput(context).GetValue(context).Float - Input2.GetOutput(context).GetValue(context).Float) < MaxDiff));
    }
}
