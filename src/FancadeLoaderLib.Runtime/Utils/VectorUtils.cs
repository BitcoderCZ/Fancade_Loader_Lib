using MathUtils.Vectors;
using System.Numerics;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("FancadeLoaderLib.Runtime.Bullet")]

namespace FancadeLoaderLib.Runtime.Utils;

internal static class VectorUtils
{
    private const float DegToRad = MathF.PI / 180f;

    public static Vector3 ToNumerics(this float3 value)
        => new Vector3(value.X, value.Y, value.Z);

    public static float3 ToFloat3(this Vector3 value)
        => new float3(value.X, value.Y, value.Z);

    public static Quaternion ToQuatDeg(this float3 value)
        => Quaternion.CreateFromYawPitchRoll(value.Y * DegToRad, value.X * DegToRad, value.Z * DegToRad);

    public static bool IsInfOrNaN(this float3 value)
        => float.IsNaN(value.X) || float.IsInfinity(value.X) || float.IsNaN(value.Y) || float.IsInfinity(value.Y) || float.IsNaN(value.Z) || float.IsInfinity(value.Z);

    public static bool IsInfOrNaN(this Vector3 value)
        => float.IsNaN(value.X) || float.IsInfinity(value.X) || float.IsNaN(value.Y) || float.IsInfinity(value.Y) || float.IsNaN(value.Z) || float.IsInfinity(value.Z);
}
