using BitcoderCZ.Maths.Vectors;
using System.Numerics;

namespace BitcoderCZ.Fancade.Runtime.Utils;

internal static class VectorUtils
{
    private const float DegToRad = MathF.PI / 180f;

    public static Vector3 ToNumerics(this float3 value)
        => new Vector3(value.X, value.Y, value.Z);

    public static float3 ToFloat3(this Vector3 value)
        => new float3(value.X, value.Y, value.Z);

    public static Quaternion ToQuatDeg(this float3 value)
        => Quaternion.CreateFromYawPitchRoll(value.Y * DegToRad, value.X * DegToRad, value.Z * DegToRad);
}
