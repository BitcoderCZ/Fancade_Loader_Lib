using System.Numerics;

namespace BitcoderCZ.Fancade.Runtime.Utils;

internal static class VectorUtils
{
    private const float DegToRad = MathF.PI / 180f;

    public static Quaternion ToQuatDeg(this Vector3 value)
        => Quaternion.CreateFromYawPitchRoll(value.Y * DegToRad, value.X * DegToRad, value.Z * DegToRad);

    public static bool IsInfOrNaN(this Vector3 value)
        => float.IsNaN(value.X) || float.IsInfinity(value.X) || float.IsNaN(value.Y) || float.IsInfinity(value.Y) || float.IsNaN(value.Z) || float.IsInfinity(value.Z);

    public static bool IsInfOrNaN(this Quaternion value)
        => float.IsNaN(value.X) || float.IsInfinity(value.X) || float.IsNaN(value.Y) || float.IsInfinity(value.Y) || float.IsNaN(value.Z) || float.IsInfinity(value.Z) || float.IsNaN(value.W) || float.IsInfinity(value.W);
}
