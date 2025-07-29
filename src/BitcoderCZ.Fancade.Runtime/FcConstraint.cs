using static BitcoderCZ.Fancade.Utils.ThrowHelper;

namespace BitcoderCZ.Fancade.Runtime;

/// <summary>
/// Represents a fancade constraint.
/// </summary>
public readonly struct FcConstraint : IEquatable<FcConstraint>
{
    /// <summary>
    /// A <see cref="FcConstraint"/> instance that represents no constraint.
    /// </summary>
    public static readonly FcConstraint Null = new FcConstraint(0);

    /// <summary>
    /// Value (id) of the <see cref="FcConstraint"/>.
    /// </summary>
    public readonly int Value;

    /// <summary>
    /// Initializes a new instance of the <see cref="FcConstraint"/> struct.
    /// </summary>
    /// <param name="value">Value (id) of the <see cref="FcConstraint"/>.</param>
    public FcConstraint(int value)
    {
        ThrowIfNegative(value, nameof(value));

        Value = value;
    }

    /// <summary>
    /// Converts an <see langword="int"/> to <see cref="MaxBuyCount"/>.
    /// </summary>
    /// <param name="value">The <see langword="int"/> to convert.</param>
    public static implicit operator FcConstraint(int value)
        => new FcConstraint(value);

    /// <summary>
    /// Converts a <see cref="FcConstraint"/> to <see langword="int"/>.
    /// </summary>
    /// <param name="value">The <see langword="FcConstraint"/> to convert.</param>
    public static explicit operator int(FcConstraint value)
        => value.Value;

    /// <summary>Returns a value that indicates whether the 2 <see cref="FcConstraint"/>s are equal.</summary>
    /// <param name="left">The first <see cref="FcConstraint"/> to compare.</param>
    /// <param name="right">The second <see cref="FcConstraint"/> to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(FcConstraint left, FcConstraint right)
        => left.Value == right.Value;

    /// <summary>Returns a value that indicates whether the 2 <see cref="FcConstraint"/>s are not equal.</summary>
    /// <param name="left">The first <see cref="FcConstraint"/> to compare.</param>
    /// <param name="right">The second <see cref="FcConstraint"/> to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(FcConstraint left, FcConstraint right)
        => left.Value != right.Value;

    /// <inheritdoc/>
    public bool Equals(FcConstraint other)
        => this == other;

    /// <inheritdoc/>
    public override int GetHashCode()
        => Value;

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj is FcConstraint other && Equals(other);
}
