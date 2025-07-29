// <copyright file="TerminalDef.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MathUtils.Vectors;

namespace BitcoderCZ.Fancade.Editing;

/// <summary>
/// Represents a fancade block terminal.
/// </summary>
public sealed class TerminalDef
{
    /// <summary>
    /// The signal type of the terminal.
    /// </summary>
    public readonly SignalType SignalType;

    /// <summary>
    /// Type of the terminal.
    /// </summary>
    public readonly TerminalType Type;

    /// <summary>
    /// Name of the terminal.
    /// </summary>
    public readonly string? Name;

    /// <summary>
    /// Index of the terminal.
    /// </summary>
    public readonly int Index;

    /// <summary>
    /// Position of the terminal.
    /// </summary>
    public readonly byte3 Position;

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalDef"/> class.
    /// </summary>
    /// <param name="signalType">Signal type of the terminal.</param>
    /// <param name="type">Type of the terminal.</param>
    /// <param name="index">Index of the terminal.</param>
    /// <param name="position">Position of the terminal.</param>
    public TerminalDef(SignalType signalType, TerminalType type, int index, byte3 position)
        : this(signalType, type, null, index, position)
    {
        Index = index;
        Position = position;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalDef"/> class.
    /// </summary>
    /// <param name="signalType">Signal type of the terminal.</param>
    /// <param name="type">Type of the terminal.</param>
    /// <param name="name">Name of the terminal.</param>
    /// <param name="index">Index of the terminal.</param>
    /// <param name="position">Position of the terminal.</param>
    public TerminalDef(SignalType signalType, TerminalType type, string? name, int index, byte3 position)
    {
        SignalType = signalType;
        Type = type;
        Name = name;
        Index = index;
        Position = position;
    }

    /// <summary>
    /// Gets the position of the after terminal of a stock script block.
    /// </summary>
    /// <value>Position of the after terminal of a stock script block.</value>
    public static byte3 AfterPosition => new byte3(3, 1, 0);

    /// <summary>
    /// Gets the default terminal name for a given <see cref="BitcoderCZ.Fancade.SignalType"/>.
    /// </summary>
    /// <param name="type">The <see cref="BitcoderCZ.Fancade.SignalType"/>.</param>
    /// <returns>The default name for <paramref name="type"/>.</returns>
    public static string GetDefaultName(SignalType type)
        => type.ToNotPointer() switch
        {
            SignalType.Void => "Execute",
            SignalType.Float => "Number",
            SignalType.Vec3 => "Vector",
            SignalType.Rot => "Rotation",
            SignalType.Bool => "Truth",
            SignalType.Obj => "Object",
            SignalType.Con => "Constraint",
            _ => string.Empty,
        };

    /// <summary>
    /// Gets the position of the before terminal of a stock script block.
    /// </summary>
    /// <param name="sizeZ">Size of the block in segments along the z axis.</param>
    /// <returns>Position of the before terminal of a stock script block of the specified size.</returns>
    public static byte3 GetBeforePosition(int sizeZ)
        => new byte3(3, 1, (sizeZ * 8) - 2);

    /// <summary>
    /// Gets the position of an input terminal of a stock script block.
    /// </summary>
    /// <param name="index">Position of the input terminal from +Z to -Z.</param>
    /// <param name="sizeZ">Size of the block in segments along the z axis.</param>
    /// <returns>Position of an input terminal of a stock script block of the specified size.</returns>
    public static byte3 GetInPosition(int index, int sizeZ)
        => new byte3(0, 1, ((sizeZ - 1 - index) * 8) + 3);

    /// <summary>
    /// Gets the position of an output terminal of a stock script block.
    /// </summary>
    /// <param name="index">Position of the output terminal from +Z to -Z.</param>
    /// <param name="sizeX">Size of the block in segments along the x axis.</param>
    /// <param name="sizeZ">Size of the block in segments along the z axis.</param>
    /// <returns>Position of an output terminal of a stock script block of the specified size.</returns>
    public static byte3 GetOutPosition(int index, int sizeX, int sizeZ)
        => new byte3((sizeX * 8) - 2, 1, ((sizeZ - 1 - index) * 8) + 3);
}
