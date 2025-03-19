// <copyright file="BlockType.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FancadeLoaderLib.Editing;

public enum BlockType
{
    /// <summary>
    /// Doesn't have any terminals.
    /// </summary>
    NonScript,

    /// <summary>
    /// Has "Before" and "After" terminals.
    /// </summary>
    Active,

    /// <summary>
    /// Doesn't have "Before" and "After" terminals.
    /// </summary>
    Pasive,

    /// <summary>
    /// Only has out terminals.
    /// </summary>
    Value,
}