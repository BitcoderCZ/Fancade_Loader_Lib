using static BitcoderCZ.Fancade.Utils.ThrowHelper;

namespace BitcoderCZ.Fancade.Editing.Scripting.Settings;

/// <summary>
/// Represents how many times a menu item can be bought.
/// </summary>
public readonly struct MaxBuyCount : IEquatable<MaxBuyCount>
{
    /// <summary>
    /// A <see cref="MaxBuyCount"/> instance that indicates that the item can be bought only once and toggled on and off.
    /// </summary>
    public static readonly MaxBuyCount OnOff = new MaxBuyCount(1);

    /// <summary>
    /// A <see cref="MaxBuyCount"/> instance that indicates that the item can be bought an infinite amount of times.
    /// </summary>
    public static readonly MaxBuyCount NoLimit = new MaxBuyCount(101);

    /// <summary>
    /// Value of the <see cref="MaxBuyCount"/>.
    /// </summary>
    public readonly int Value;

    /// <summary>
    /// Initializes a new instance of the <see cref="MaxBuyCount"/> struct.
    /// </summary>
    /// <param name="value">Value of the <see cref="MaxBuyCount"/>, must be between 0 and 101.</param>
    public MaxBuyCount(int value)
    {
        ThrowIfNegative(value);
        if (value > 101)
        {
            ThrowArgumentOutOfRangeException($"{nameof(value)} must be less than or equal to 101.", nameof(value));
        }

        Value = value;
    }

    /// <summary>
    /// Converts an <see langword="int"/> to <see cref="MaxBuyCount"/>.
    /// </summary>
    /// <param name="value">The <see langword="int"/> to convert.</param>
    public static implicit operator MaxBuyCount(int value)
        => new MaxBuyCount(value);

    /// <summary>
    /// Converts a <see cref="MaxBuyCount"/> to <see langword="int"/>.
    /// </summary>
    /// <param name="value">The <see cref="MaxBuyCount"/> to convert.</param>
    public static explicit operator int(MaxBuyCount value)
        => value.Value;

    /// <summary>Returns a value that indicates whether the 2 <see cref="MaxBuyCount"/>s are equal.</summary>
    /// <param name="left">The first <see cref="MaxBuyCount"/> to compare.</param>
    /// <param name="right">The second <see cref="MaxBuyCount"/> to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(MaxBuyCount left, MaxBuyCount right)
        => left.Value == right.Value;

    /// <summary>Returns a value that indicates whether the 2 <see cref="MaxBuyCount"/>s are not equal.</summary>
    /// <param name="left">The first <see cref="MaxBuyCount"/> to compare.</param>
    /// <param name="right">The second <see cref="MaxBuyCount"/> to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(MaxBuyCount left, MaxBuyCount right)
        => left.Value != right.Value;

    /// <inheritdoc/>
    public bool Equals(MaxBuyCount other)
        => this == other;

    /// <inheritdoc/>
    public override int GetHashCode()
        => Value;

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj is MaxBuyCount other && Equals(other);
}
