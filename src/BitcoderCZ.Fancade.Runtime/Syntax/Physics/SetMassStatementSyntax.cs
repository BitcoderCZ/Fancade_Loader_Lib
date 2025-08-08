using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Physics;

/// <summary>
/// A <see cref="SyntaxNode"/> for the set mass prefab.
/// </summary>
public sealed class SetMassStatementSyntax : StatementSyntax
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SetMassStatementSyntax"/> class.
    /// </summary>
    /// <param name="prefabId">Id of the prefab this node represents.</param>
    /// <param name="position">Position of the prefab this node represents.</param>
    /// <param name="outVoidConnections">Output void connections from this node.</param>
    /// <param name="object">The object terminal; or <see langword="null"/>, if it is not connected.</param>
    /// <param name="mass">The mass terminal; or <see langword="null"/>, if it is not connected.</param>
    public SetMassStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? @object, SyntaxTerminal? mass)
        : base(prefabId, position, outVoidConnections)
    {
        Object = @object;
        Mass = mass;
    }

    /// <summary>
    /// Gets the object terminal.
    /// </summary>
    /// <value>The object terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? Object { get; }

    /// <summary>
    /// Gets the mass terminal.
    /// </summary>
    /// <value>The mass terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? Mass { get; }

    /// <inheritdoc/>
    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(2)];
}
