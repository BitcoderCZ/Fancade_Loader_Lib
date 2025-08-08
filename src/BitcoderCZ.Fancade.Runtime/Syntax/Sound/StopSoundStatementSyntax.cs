using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Sound;

/// <summary>
/// A <see cref="SyntaxNode"/> for the stop sound prefab.
/// </summary>
public sealed class StopSoundStatementSyntax : StatementSyntax
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StopSoundStatementSyntax"/> class.
    /// </summary>
    /// <param name="prefabId">Id of the prefab this node represents.</param>
    /// <param name="position">Position of the prefab this node represents.</param>
    /// <param name="outVoidConnections">Output void connections from this node.</param>
    /// <param name="channel">The channel terminal; or <see langword="null"/>, if it is not connected.</param>
    public StopSoundStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? channel)
        : base(prefabId, position, outVoidConnections)
    {
        Channel = channel;
    }

    /// <summary>
    /// Gets the channel terminal.
    /// </summary>
    /// <value>The channel terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? Channel { get; }

    /// <inheritdoc/>
    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(2)];
}
