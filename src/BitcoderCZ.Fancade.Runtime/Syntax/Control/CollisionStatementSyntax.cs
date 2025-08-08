using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Control;

/// <summary>
/// A <see cref="SyntaxNode"/> for the collision prefab.
/// </summary>
public sealed class CollisionStatementSyntax : StatementSyntax
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CollisionStatementSyntax"/> class.
    /// </summary>
    /// <param name="prefabId">Id of the prefab this node represents.</param>
    /// <param name="position">Position of the prefab this node represents.</param>
    /// <param name="outVoidConnections">Output void connections from this node.</param>
    /// <param name="firstObject">The first object terminal; or <see langword="null"/>, if it is not connected.</param>
    public CollisionStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? firstObject)
        : base(prefabId, position, outVoidConnections)
    {
        FirstObject = firstObject;
    }

    /// <summary>
    /// Gets the first object terminal.
    /// </summary>
    /// <value>The first object terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? FirstObject { get; }

    /// <inheritdoc/>
    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(4)];
}
