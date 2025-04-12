using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Runtime.Utils;
using MathUtils.Vectors;
using System.Diagnostics;
using System.Numerics;

namespace FancadeLoaderLib.Runtime.Functions.Math;

public sealed class LookRotationFunction : BinaryFunction
{
    public LookRotationFunction(RuntimeTerminal input1, RuntimeTerminal input2)
        : base(input1, input2)
    {
    }

    public override TerminalOutput GetTerminalOutput(byte3 terminalPos, IRuntimeContext context)
    {
        Debug.Assert(terminalPos == TerminalDef.GetOutPosition(0, 2, 2), $"{nameof(terminalPos)} should be valid.");

        Vector3 forward = Input1.GetOutput(context).GetValue(context).Float3.ToNumerics();

        if (forward == Vector3.Zero)
        {
            return new TerminalOutput(new RuntimeValue(Quaternion.Identity));
        }

        var upOut = Input2.GetOutput(context);
        Vector3 up = upOut.IsConnected ? upOut.GetValue(context).Float3.ToNumerics() : Vector3.UnitY;

        forward = Vector3.Normalize(forward);
        up = Vector3.Normalize(up);

        Vector3 right = Vector3.Cross(up, forward);
        if (right == Vector3.Zero)
        {
            right = Vector3.UnitX;
        }
        else
        {
            right = Vector3.Normalize(right);
        }

        up = Vector3.Cross(forward, right);

#pragma warning disable SA1117 // Parameters should be on same line or separate lines
        Matrix4x4 rotationMatrix = new Matrix4x4(
            right.X, right.Y, right.Z, 0,
            up.X, up.Y, up.Z, 0,
            forward.X, forward.Y, forward.Z, 0,
            0, 0, 0, 1);
#pragma warning restore SA1117 // Parameters should be on same line or separate lines

        return new TerminalOutput(new RuntimeValue(Quaternion.CreateFromRotationMatrix(rotationMatrix)));
    }
}
