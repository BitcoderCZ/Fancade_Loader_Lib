// <copyright file="Rotation.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade;

/// <summary>
/// Wrapper over <see cref="float3"/> to represent rotation.
/// </summary>
public readonly struct Rotation : IEquatable<Rotation>
{
    /// <summary>
    /// The value of this rotation.
    /// </summary>
    public readonly float3 Value;

    /// <summary>
    /// Initializes a new instance of the <see cref="Rotation"/> struct.
    /// </summary>
    /// <param name="value">Value of this rotation.</param>
    public Rotation(float3 value)
    {
        Value = value;
    }

    /// <summary>
    /// Converts a <see cref="Rotation"/> to a <see cref="float3"/>.
    /// </summary>
    /// <param name="value">The <see cref="Rotation"/> to convert.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2225:Operator overloads have named alternates", Justification = "Use the value field.")]
    public static explicit operator float3(Rotation value)
        => value.Value;

    /// <summary>
    /// Converts a <see cref="float3"/> to a <see cref="Rotation"/>.
    /// </summary>
    /// <param name="value">The <see cref="float3"/> to convert.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2225:Operator overloads have named alternates", Justification = "Use the constructor.")]
    public static explicit operator Rotation(float3 value)
        => new Rotation(value);

    /// <summary>Returns a value that indicates whether the 2 <see cref="Rotation"/>s are equal.</summary>
    /// <param name="left">The first <see cref="Rotation"/> to compare.</param>
    /// <param name="right">The second <see cref="Rotation"/> to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(Rotation left, Rotation right)
        => left.Value == right.Value;

    /// <summary>Returns a value that indicates whether the 2 <see cref="Rotation"/>s are not equal.</summary>
    /// <param name="left">The first <see cref="Rotation"/> to compare.</param>
    /// <param name="right">The second <see cref="Rotation"/> to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(Rotation left, Rotation right)
        => left.Value != right.Value;

    /// <inheritdoc/>
    public readonly bool Equals(Rotation other)
        => this == other;

    /// <inheritdoc/>
    public readonly override bool Equals(object? obj)
        => obj is Rotation other && this == other;

    /// <inheritdoc/>
    public readonly override int GetHashCode()
        => Value.GetHashCode();
}
