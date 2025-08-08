using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Math;

/// <summary>
/// A <see cref="SyntaxNode"/> for the random seed prefab.
/// </summary>
public sealed class RandomSeedStatementSyntax : StatementSyntax
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RandomSeedStatementSyntax"/> class.
    /// </summary>
    /// <param name="prefabId">Id of the prefab this node represents.</param>
    /// <param name="position">Position of the prefab this node represents.</param>
    /// <param name="outVoidConnections">Output void connections from this node.</param>
    /// <param name="seed">The seed terminal; or <see langword="null"/>, if it is not connected.</param>
    public RandomSeedStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? seed)
        : base(prefabId, position, outVoidConnections)
    {
        Seed = seed;
    }

    /// <summary>
    /// Gets the seed terminal.
    /// </summary>
    /// <value>The seed terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? Seed { get; }

    /// <inheritdoc/>
    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(2)];
}
