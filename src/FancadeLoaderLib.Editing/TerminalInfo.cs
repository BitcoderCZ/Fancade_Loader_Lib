using MathUtils.Vectors;
using System;

namespace FancadeLoaderLib.Editing;

/// <summary>
/// Info about a terminal.
/// </summary>
public readonly struct TerminalInfo : IEquatable<TerminalInfo>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalInfo"/> struct.
    /// </summary>
    /// <param name="position">Position of the terminal.</param>
    /// <param name="type">Type of the terminal.</param>
    /// <param name="direction">Direction of the terminal.</param>
    /// <param name="isInput"><see langword="true"/> if the terminal is input; <see langword="false"/> if the terminal is output.</param>
    public TerminalInfo(byte3 position, SignalType type, TerminalDirection direction, bool isInput)
    {
        Position = position;
        Type = type;
        Direction = direction;
        IsInput = isInput;
    }

    /// <summary>
    /// Gets the position of the terminal.
    /// </summary>
    /// <value>Position of the terminal.</value>
    public readonly byte3 Position { get; }

    /// <summary>
    /// Gets the type of the terminal.
    /// </summary>
    /// <value>Type of the terminal.</value>
    public readonly SignalType Type { get; }

    /// <summary>
    /// Gets the direction of the terminal.
    /// </summary>
    /// <value>Direction of the terminal.</value>
    public readonly TerminalDirection Direction { get; }

    /// <summary>
    /// Gets a value indicating whether the terminal is input.
    /// </summary>
    /// <value><see langword="true"/> if the terminal is input; <see langword="false"/> if the terminal is output.</value>
    public readonly bool IsInput { get; }

    /// <summary>Returns a value that indicates whether the 2 <see cref="TerminalInfo"/>s are equal.</summary>
    /// <param name="left">The first <see cref="TerminalInfo"/> to compare.</param>
    /// <param name="right">The second <see cref="TerminalInfo"/> to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(TerminalInfo left, TerminalInfo right)
        => left.Position == right.Position && left.Type == right.Type && left.Direction == right.Direction;

    /// <summary>Returns a value that indicates whether the 2 <see cref="TerminalInfo"/>s are not equal.</summary>
    /// <param name="left">The first <see cref="TerminalInfo"/> to compare.</param>
    /// <param name="right">The second <see cref="TerminalInfo"/> to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(TerminalInfo left, TerminalInfo right)
        => left.Position != right.Position || left.Type != right.Type || left.Direction != right.Direction;

    /// <inheritdoc/>
    public bool Equals(TerminalInfo other)
        => other == this;

    /// <inheritdoc/>
    public override int GetHashCode()
        => HashCode.Combine(Position, Type, Direction);

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj is TerminalInfo other && other == this;
}
