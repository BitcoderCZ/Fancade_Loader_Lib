// <copyright file="TerminalDef.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MathUtils.Vectors;

namespace FancadeLoaderLib.Editing;

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
    public readonly int3 Position;

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalDef"/> class.
    /// </summary>
    /// <param name="signalType">Signal type of the terminal.</param>
    /// <param name="type">Type of the terminal.</param>
    /// <param name="index">Index of the terminal.</param>
    /// <param name="position">Position of the terminal.</param>
    public TerminalDef(SignalType signalType, TerminalType type, int index, int3 position)
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
    public TerminalDef(SignalType signalType, TerminalType type, string? name, int index, int3 position)
    {
        SignalType = signalType;
        Type = type;
        Name = name;
        Index = index;
        Position = position;
    }
}
