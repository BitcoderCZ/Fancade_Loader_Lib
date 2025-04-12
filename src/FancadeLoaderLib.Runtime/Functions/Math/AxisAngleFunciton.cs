using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Runtime.Utils;
using MathUtils.Vectors;
using System.Diagnostics;
using System.Numerics;

namespace FancadeLoaderLib.Runtime.Functions.Math;

public sealed class AxisAngleFunciton : BinaryFunction
{
    public AxisAngleFunciton(RuntimeTerminal input1, RuntimeTerminal input2)
        : base(input1, input2)
    {
    }

    public override TerminalOutput GetTerminalOutput(byte3 terminalPos, IRuntimeContext context)
    {
        Debug.Assert(terminalPos == TerminalDef.GetOutPosition(0, 2, 2), $"{nameof(terminalPos)} should be valid.");

        Vector3 axis = Input1.GetOutput(context).GetValue(context).Float3.ToNumerics();
        float angle = Input2.GetOutput(context).GetValue(context).Float * (MathF.PI / 180f);

#if NET6_0_OR_GREATER
        var (sin, cos) = MathF.SinCos(angle * 0.5f);
#else
        float sin = MathF.Sin(angle * 0.5f);
        float cos = MathF.Cos(angle * 0.5f);
#endif

        return new TerminalOutput(new RuntimeValue(Quaternion.Normalize(new Quaternion(axis.X * sin, axis.Y * sin, axis.Z * sin, cos))));
    }
}
