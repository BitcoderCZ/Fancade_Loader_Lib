using BitcoderCZ.Maths.Vectors;
using System.Numerics;

namespace BitcoderCZ.Fancade.Runtime.Utils;

internal static class VectorUtils
{
    private const float DegToRad = MathF.PI / 180f;

    public static Vector3 ToNumerics(this Vector3 value)
        => new Vector3(value.X, value.Y, value.Z);

    public static Vector3 ToFloat3(this Vector3 value)
        => new Vector3(value.X, value.Y, value.Z);

    public static Quaternion ToQuatDeg(this Vector3 value)
        => Quaternion.CreateFromYawPitchRoll(value.Y * DegToRad, value.X * DegToRad, value.Z * DegToRad);
}
