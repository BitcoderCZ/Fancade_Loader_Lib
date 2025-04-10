using FancadeLoaderLib.Editing;
using MathUtils.Vectors;
using System.Diagnostics;

namespace FancadeLoaderLib.Runtime.Functions.Math;

public sealed class DotProductFunction : BinaryFunction
{
    public DotProductFunction(RuntimeTerminal input1, RuntimeTerminal input2)
        : base(input1, input2)
    {
    }

    public override TerminalOutput GetTerminalOutput(byte3 terminalPos, IRuntimeContext context)
    {
        Debug.Assert(terminalPos == TerminalDef.GetOutPosition(0, 2, 2), $"{nameof(terminalPos)} should be valid.");

        return new TerminalOutput(new RuntimeValue(float3.Dot(Input1.GetOutput(context).GetValue(context).Float3, Input2.GetOutput(context).GetValue(context).Float3)));
    }
}
