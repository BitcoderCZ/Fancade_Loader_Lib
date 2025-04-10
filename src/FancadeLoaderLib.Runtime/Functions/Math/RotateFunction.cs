using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Runtime.Utils;
using MathUtils.Vectors;
using System.Diagnostics;
using System.Numerics;

namespace FancadeLoaderLib.Runtime.Functions.Math;

public sealed class RotateFunction : BinaryFunction
{
    public RotateFunction(RuntimeTerminal input1, RuntimeTerminal input2)
        : base(input1, input2)
    {
    }

    public override TerminalOutput GetTerminalOutput(byte3 terminalPos, IRuntimeContext context)
    {
        Debug.Assert(terminalPos == TerminalDef.GetOutPosition(0, 2, 2), $"{nameof(terminalPos)} should be valid.");

        return new TerminalOutput(new RuntimeValue(Vector3.Transform(Input1.GetOutput(context).GetValue(context).Float3.ToNumerics(), Input2.GetOutput(context).GetValue(context).Quaternion).ToFloat3()));
    }
}
