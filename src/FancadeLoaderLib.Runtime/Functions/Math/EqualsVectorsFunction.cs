using FancadeLoaderLib.Editing;
using MathUtils.Vectors;
using System.Diagnostics;

namespace FancadeLoaderLib.Runtime.Functions.Math;

public sealed class EqualsVectorsFunction : BinaryFunction
{
    public EqualsVectorsFunction(RuntimeTerminal input1, RuntimeTerminal input2)
        : base(input1, input2)
    {
    }

    public override TerminalOutput GetTerminalOutput(byte3 terminalPos, IRuntimeContext context)
    {
        Debug.Assert(terminalPos == TerminalDef.GetOutPosition(0, 2, 2), $"{nameof(terminalPos)} should be valid.");

        const float MaxDiff = 1.0000001e-06f;

        float distSquared = (Input1.GetOutput(context).GetValue(context).Float3 - Input2.GetOutput(context).GetValue(context).Float3).LengthSquared;

        return new TerminalOutput(new RuntimeValue(distSquared < MaxDiff));
    }
}
