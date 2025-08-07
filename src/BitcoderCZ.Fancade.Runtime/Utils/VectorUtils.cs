using System.Numerics;

namespace BitcoderCZ.Fancade.Runtime.Utils;

internal static class VectorUtils
{
    private const float DegToRad = MathF.PI / 180f;

    public static Quaternion ToQuatDeg(this Vector3 value)
        => Quaternion.CreateFromYawPitchRoll(value.Y * DegToRad, value.X * DegToRad, value.Z * DegToRad);
}
