using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Physics;

/// <summary>
/// A <see cref="SyntaxNode"/> for the set bounciness prefab.
/// </summary>
public sealed class SetBouncinessStatementSyntax : StatementSyntax
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SetBouncinessStatementSyntax"/> class.
    /// </summary>
    /// <param name="prefabId">Id of the prefab this node represents.</param>
    /// <param name="position">Position of the prefab this node represents.</param>
    /// <param name="outVoidConnections">Output void connections from this node.</param>
    /// <param name="object">The object terminal; or <see langword="null"/>, if it is not connected.</param>
    /// <param name="bounciness">The bounciness terminal; or <see langword="null"/>, if it is not connected.</param>
    public SetBouncinessStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? @object, SyntaxTerminal? bounciness)
        : base(prefabId, position, outVoidConnections)
    {
        Object = @object;
        Bounciness = bounciness;
    }

    /// <summary>
    /// Gets the object terminal.
    /// </summary>
    /// <value>The object terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? Object { get; }

    /// <summary>
    /// Gets the bounciness terminal.
    /// </summary>
    /// <value>The bounciness terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? Bounciness { get; }

    /// <inheritdoc/>
    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(2)];
}
