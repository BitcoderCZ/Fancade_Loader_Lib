// <copyright file="UnsupportedVersionException.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Fancade.Raw;

namespace BitcoderCZ.Fancade.Exceptions;

/// <summary>
/// Thrown when an attempt to load a game of an unsupported version is made.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "Not desirable for this type.")]
public sealed class UnsupportedVersionException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnsupportedVersionException"/> class.
    /// </summary>
    /// <param name="version">Version of the game.</param>
    public UnsupportedVersionException(int version)
        : base(version > RawGame.CurrentFileVersion ? $"File version '{version}' isn't supported, highest supported version is {RawGame.CurrentFileVersion}." : $"File version '{version}' isn't supported.")
    {
        Version = version;
    }

    /// <summary>
    /// Gets version of the game.
    /// </summary>
    /// <value>Version of the game.</value>
    public int Version { get; private set; }
}
