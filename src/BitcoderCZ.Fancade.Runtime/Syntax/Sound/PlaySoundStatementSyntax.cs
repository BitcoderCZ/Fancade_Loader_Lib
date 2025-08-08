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
    /// <param name="prefabId">Id of the prefab this node represents.</param>
    /// <param name="position">Position of the prefab this node represents.</param>
    /// <param name="outVoidConnections">Output void connections from this node.</param>
    /// <param name="volume">The volume terminal; or <see langword="null"/>, if it is not connected.</param>
    /// <param name="pitch">The pitch terminal; or <see langword="null"/>, if it is not connected.</param>
    /// <param name="sound">The sound to play.</param>
    public PlaySoundStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? volume, SyntaxTerminal? pitch, FcSound sound)
        : base(prefabId, position, outVoidConnections)
    {
        Volume = volume;
        Pitch = pitch;
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
    /// Gets the sound to play.
    /// </summary>
    /// <value>The sound to play.</value>
    public FcSound Sound { get; }

    /// <inheritdoc/>
    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(2)];
}
