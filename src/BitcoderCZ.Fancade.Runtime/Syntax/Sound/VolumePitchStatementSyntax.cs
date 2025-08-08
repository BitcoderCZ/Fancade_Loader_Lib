using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Sound;

/// <summary>
/// A <see cref="SyntaxNode"/> for the volume pitch prefab.
/// </summary>
public sealed class VolumePitchStatementSyntax : StatementSyntax
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VolumePitchStatementSyntax"/> class.
    /// </summary>
    /// <param name="prefabId">Id of the prefab this node represents.</param>
    /// <param name="position">Position of the prefab this node represents.</param>
    /// <param name="outVoidConnections">Output void connections from this node.</param>
    /// <param name="channel">The channel terminal; or <see langword="null"/>, if it is not connected.</param>
    /// <param name="volume">The volume terminal; or <see langword="null"/>, if it is not connected.</param>
    /// <param name="pitch">The pitch terminal; or <see langword="null"/>, if it is not connected.</param>
    public VolumePitchStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? channel, SyntaxTerminal? volume, SyntaxTerminal? pitch)
        : base(prefabId, position, outVoidConnections)
    {
        Channel = channel;
        Volume = volume;
        Pitch = pitch;
    }

    /// <summary>
    /// Gets the channel terminal.
    /// </summary>
    /// <value>The channel terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? Channel { get; }

    /// <summary>
    /// Gets the volume terminal.
    /// </summary>
    /// <value>The volume terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? Volume { get; }

    /// <summary>
    /// Gets pitch value terminal.
    /// </summary>
    /// <value>The pitch terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? Pitch { get; }

    /// <inheritdoc/>
    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(3)];
}
