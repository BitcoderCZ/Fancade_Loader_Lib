// <copyright file="PlaySoundStatementSyntax.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Fancade.Editing.Scripting.Settings;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Sound;

/// <summary>
/// A <see cref="SyntaxNode"/> for the play sound prefab.
/// </summary>
public sealed class PlaySoundStatementSyntax : StatementSyntax
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlaySoundStatementSyntax"/> class.
    /// </summary>
    /// <param name="position">Position of the prefab this node represents.</param>
    /// <param name="outVoidConnections">Output void connections from this node.</param>
    /// <param name="volume">The volume terminal; or <see langword="null"/>, if it is not connected.</param>
    /// <param name="pitch">The pitch terminal; or <see langword="null"/>, if it is not connected.</param>
    /// <param name="loop"><see langword="true"/> if the sound should loop; otherwise, <see langword="false"/>.</param>
    /// <param name="sound">The sound to play.</param>
    public PlaySoundStatementSyntax(int3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? volume, SyntaxTerminal? pitch, bool loop, FcSound sound)
        : base(264, position, outVoidConnections)
    {
        Volume = volume;
        Pitch = pitch;
        Loop = loop;
        Sound = sound;
    }

    /// <summary>
    /// Gets the volume terminal.
    /// </summary>
    /// <value>The volume terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? Volume { get; }

    /// <summary>
    /// Gets the pitch terminal.
    /// </summary>
    /// <value>The pitch terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? Pitch { get; }

    /// <summary>
    /// Gets a value indicating whether the sound should loop.
    /// </summary>
    /// <value><see langword="true"/> if the sound should loop; otherwise, <see langword="false"/>.</value>
    public bool Loop { get; }

    /// <summary>
    /// Gets the sound to play.
    /// </summary>
    /// <value>The sound to play.</value>
    public FcSound Sound { get; }

    /// <inheritdoc/>
    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(2)];
}
