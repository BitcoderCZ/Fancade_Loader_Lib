using System.Numerics;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("BitcoderCZ.Fancade.Runtime.Simulated.Bullet")]

namespace BitcoderCZ.Fancade.Runtime.Utils;

internal static class VectorUtils
{
    private const float DegToRad = MathF.PI / 180f;

    public static Quaternion ToQuaternionDegrees(this Vector3 value)
        => Quaternion.CreateFromYawPitchRoll(value.Y * DegToRad, value.X * DegToRad, value.Z * DegToRad);

    public static bool IsInfOrNaN(this Vector3 value)
        => float.IsNaN(value.X) || float.IsInfinity(value.X) || float.IsNaN(value.Y) || float.IsInfinity(value.Y) || float.IsNaN(value.Z) || float.IsInfinity(value.Z);

    public static bool IsInfOrNaN(this Quaternion value)
        => float.IsNaN(value.X) || float.IsInfinity(value.X) || float.IsNaN(value.Y) || float.IsInfinity(value.Y) || float.IsNaN(value.Z) || float.IsInfinity(value.Z) || float.IsNaN(value.W) || float.IsInfinity(value.W);

    public static float GetAxis(this Vector3 value, int index)
        => index switch
        {
            0 => value.X,
            1 => value.Y,
            2 => value.Z,
            _ => throw new ArgumentOutOfRangeException(nameof(index)),
        };
}
