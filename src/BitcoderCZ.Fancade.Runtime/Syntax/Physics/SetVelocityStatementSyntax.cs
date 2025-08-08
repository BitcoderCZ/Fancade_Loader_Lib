using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Physics;

/// <summary>
/// A <see cref="SyntaxNode"/> for the set velocity prefab.
/// </summary>
public sealed class SetVelocityStatementSyntax : StatementSyntax
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SetVelocityStatementSyntax"/> class.
    /// </summary>
    /// <param name="prefabId">Id of the prefab this node represents.</param>
    /// <param name="position">Position of the prefab this node represents.</param>
    /// <param name="outVoidConnections">Output void connections from this node.</param>
    /// <param name="object">The object terminal; or <see langword="null"/>, if it is not connected.</param>
    /// <param name="velocity">The velocity terminal; or <see langword="null"/>, if it is not connected.</param>
    /// <param name="spin">The spin terminal; or <see langword="null"/>, if it is not connected.</param>
    public SetVelocityStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? @object, SyntaxTerminal? velocity, SyntaxTerminal? spin)
        : base(prefabId, position, outVoidConnections)
    {
        Object = @object;
        Velocity = velocity;
        Spin = spin;
    }

    /// <summary>
    /// Gets the object terminal.
    /// </summary>
    /// <value>The object terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? Object { get; }

    /// <summary>
    /// Gets the velocity terminal.
    /// </summary>
    /// <value>The velocity terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? Velocity { get; }

    /// <summary>
    /// Gets the spin terminal.
    /// </summary>
    /// <value>The spin terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? Spin { get; }

    /// <inheritdoc/>
    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(3)];
}
