using System.Numerics;

namespace FancadeLoaderLib.Runtime.Utils;

public sealed class QuaternionUtils
{
    public static Quaternion AxisAngle(Vector3 axis, float angle)
    {
        angle = angle * (MathF.PI / 180f);

#if NET6_0_OR_GREATER
        var (sin, cos) = MathF.SinCos(angle * 0.5f);
#else
        float sin = MathF.Sin(angle * 0.5f);
        float cos = MathF.Cos(angle * 0.5f);
#endif

        return Quaternion.Normalize(new Quaternion(axis.X * sin, axis.Y * sin, axis.Z * sin, cos));
    }

    public static Quaternion LookRotation(Vector3 forward, Vector3 up)
    {
        if (forward == Vector3.Zero)
        {
            return Quaternion.Identity;
        }

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

        return Quaternion.CreateFromRotationMatrix(rotationMatrix);
    }
}
