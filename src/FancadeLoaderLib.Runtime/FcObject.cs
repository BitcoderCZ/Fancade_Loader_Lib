using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib.Runtime;

/// <summary>
/// Represents a fancade object.
/// </summary>
public readonly struct FcObject : IEquatable<FcObject>
{
    /// <summary>
    /// A <see cref="FcObject"/> instance that represents no object.
    /// </summary>
    public static readonly FcObject Null = new FcObject(0);

    /// <summary>
    /// Value (id) of the <see cref="FcObject"/>.
    /// </summary>
    public readonly int Value;

    /// <summary>
    /// Initializes a new instance of the <see cref="FcObject"/> struct.
    /// </summary>
    /// <param name="value">Value (id) of the <see cref="FcObject"/>.</param>
    public FcObject(int value)
    {
        ThrowIfNegative(value, nameof(value));

        Value = value;
    }

    /// <summary>
    /// Converts an <see langword="int"/> to <see cref="MaxBuyCount"/>.
    /// </summary>
    /// <param name="value">The <see langword="int"/> to convert.</param>
    public static explicit operator FcObject(int value)
        => new FcObject(value);

    /// <summary>
    /// Converts a <see cref="FcObject"/> to <see langword="int"/>.
    /// </summary>
    /// <param name="value">The <see langword="FcObject"/> to convert.</param>
    public static explicit operator int(FcObject value)
        => value.Value;

    /// <summary>Returns a value that indicates whether the 2 <see cref="FcObject"/>s are equal.</summary>
    /// <param name="left">The first <see cref="FcObject"/> to compare.</param>
    /// <param name="right">The second <see cref="FcObject"/> to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(FcObject left, FcObject right)
        => left.Value == right.Value;

    /// <summary>Returns a value that indicates whether the 2 <see cref="FcObject"/>s are not equal.</summary>
    /// <param name="left">The first <see cref="FcObject"/> to compare.</param>
    /// <param name="right">The second <see cref="FcObject"/> to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(FcObject left, FcObject right)
        => left.Value != right.Value;

    /// <inheritdoc/>
    public bool Equals(FcObject other)
        => this == other;

    /// <inheritdoc/>
    public override int GetHashCode()
        => Value;

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj is FcObject other && Equals(other);
}
