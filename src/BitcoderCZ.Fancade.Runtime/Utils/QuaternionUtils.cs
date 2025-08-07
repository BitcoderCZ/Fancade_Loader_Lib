using System.Numerics;

namespace BitcoderCZ.Fancade.Runtime.Utils;

/// <summary>
/// Utils for <see cref="Quaternion"/>.
/// </summary>
public static class QuaternionUtils
{
    /// <summary>
    /// Creates a <see cref="Quaternion"/> from a unit vector and an angle to rotate around the vector.
    /// </summary>
    /// <param name="axis">The unit vector to rotate around.</param>
    /// <param name="angle">The angle, in degrees, to rotate around the vector.</param>
    /// <returns>The newly created <see cref="Quaternion"/>.</returns>
    public static Quaternion AxisAngle(Vector3 axis, float angle)
    {
        angle = angle * (MathF.PI / 180f);

#if NET6_0_OR_GREATER
        var (sin, cos) = MathF.SinCos(angle * 0.5f);
#else
        float sin = MathF.Sin(angle * 0.5f);
        float cos = MathF.Cos(angle * 0.5f);
#endif
        Quaternion.CreateFromAxisAngle(axis, angle);
        return Quaternion.Normalize(new Quaternion(axis.X * sin, axis.Y * sin, axis.Z * sin, cos));
    }

    /// <summary>
    /// Creates a <see cref="Quaternion"/> from forward and up vectors.
    /// </summary>
    /// <param name="forward">The forward vector.</param>
    /// <param name="up">The up vector.</param>
    /// <returns>The newly created <see cref="Quaternion"/>.</returns>
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

    /// <summary>
    /// Converts a <see cref="Quaternion"/> to Euler angles (in degrees).
    /// </summary>
    /// <param name="rot">The <see cref="Quaternion"/> to convert.</param>
    /// <returns>
    /// A <see cref="Vector3"/> representing the Euler angles in degrees.
    /// </returns>
    public static Vector3 GetEuler(this Quaternion rot)
        => new Vector3(rot.GetEulerX(), rot.GetEulerY(), rot.GetEulerZ());

    /// <summary>
    /// Calculates the pitch in degrees from a <see cref="Quaternion"/>.
    /// </summary>
    /// <param name="rot">The <see cref="Quaternion"/> to extract the pitch from.</param>
    /// <returns>
    /// The pitch angle in degrees.
    /// </returns>
    public static float GetEulerX(this Quaternion rot)
    {
        float pitchSin = 2.0f * ((rot.W * rot.Y) - (rot.Z * rot.X));

        return pitchSin > 1.0f ? 90f : pitchSin < -1.0f ? -90f : MathF.Asin(pitchSin) * (180f / MathF.PI);
    }

    /// <summary>
    /// Calculates the yaw in degrees from a <see cref="Quaternion"/>.
    /// </summary>
    /// <param name="rot">The <see cref="Quaternion"/> to extract the yaw from.</param>
    /// <returns>
    /// The yaw angle in degrees.
    /// </returns>
    public static float GetEulerY(this Quaternion rot)
    {
        float xx = rot.X * rot.X;
        float yy = rot.Y * rot.Y;
        float zz = rot.Z * rot.Z;
        float ww = rot.W * rot.W;

        return MathF.Atan2(2.0f * ((rot.Y * rot.Z) + (rot.W * rot.X)), ww + xx - yy - zz) * (180f / MathF.PI);
    }

    /// <summary>
    /// Calculates the roll in degrees from a <see cref="Quaternion"/>.
    /// </summary>
    /// <param name="rot">The <see cref="Quaternion"/> to extract the roll from.</param>
    /// <returns>
    /// The roll angle in degrees.
    /// </returns>
    public static float GetEulerZ(this Quaternion rot)
    {
        float xx = rot.X * rot.X;
        float yy = rot.Y * rot.Y;
        float zz = rot.Z * rot.Z;
        float ww = rot.W * rot.W;

        return MathF.Atan2(2.0f * ((rot.X * rot.Y) + (rot.W * rot.Z)), ww - xx - yy + zz) * (180f / MathF.PI);
    }
}
