// <copyright file="VectorUtils.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System.Numerics;
using System.Runtime.CompilerServices;

namespace BitcoderCZ.Fancade.Runtime.Utils;

/// <summary>
/// Utils for <see cref="Vector3"/> and <see cref="Quaternion"/>.
/// </summary>
public static class VectorUtils
{
    private const float DegToRad = MathF.PI / 180f;

    /// <summary>
    /// Converts a <see cref="Vector3"/> to <see cref="Quaternion"/>.
    /// </summary>
    /// <param name="value">The <see cref="Vector3"/> to convert.</param>
    /// <returns>The converted <see cref="Quaternion"/>.</returns>
    public static Quaternion ToQuaternionDegrees(this Vector3 value)
        => Quaternion.CreateFromYawPitchRoll(value.Y * DegToRad, value.X * DegToRad, value.Z * DegToRad);

    /// <summary>
    /// Get whether any of the axes are <see cref="float.IsInfinity"/> or <see cref="float.IsNaN"/>.
    /// </summary>
    /// <param name="value">The vector to check.</param>
    /// <returns><see langword="true"/> if any of the exes are infinite or NaN; otherwise, <see langword="false"/>.</returns>
    public static bool IsInfOrNaN(this Vector3 value)
        => float.IsNaN(value.X) || float.IsInfinity(value.X) || float.IsNaN(value.Y) || float.IsInfinity(value.Y) || float.IsNaN(value.Z) || float.IsInfinity(value.Z);

    /// <summary>
    /// Get whether any of the axes are <see cref="float.IsInfinity"/> or <see cref="float.IsNaN"/>.
    /// </summary>
    /// <param name="value">The quaternion to check.</param>
    /// <returns><see langword="true"/> if any of the exes are infinite or NaN; otherwise, <see langword="false"/>.</returns>
    public static bool IsInfOrNaN(this Quaternion value)
        => float.IsNaN(value.X) || float.IsInfinity(value.X) || float.IsNaN(value.Y) || float.IsInfinity(value.Y) || float.IsNaN(value.Z) || float.IsInfinity(value.Z) || float.IsNaN(value.W) || float.IsInfinity(value.W);

    /// <summary>
    /// Gets the specified axis of a <see cref="Vector3"/>.
    /// </summary>
    /// <param name="value">The <see cref="Vector3"/>.</param>
    /// <param name="index">Index of the axis; X - 0, Y - 1, Z - 2.</param>
    /// <returns>Value of the specified axis.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Throw when <paramref name="index"/> is out of range.</exception>
    public static float GetAxis(this Vector3 value, int index)
        => index switch
        {
            0 => value.X,
            1 => value.Y,
            2 => value.Z,
            _ => throw new ArgumentOutOfRangeException(nameof(index)),
        };
}
