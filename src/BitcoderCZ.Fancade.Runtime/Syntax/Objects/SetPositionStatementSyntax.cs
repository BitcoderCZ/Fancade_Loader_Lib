using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Objects;

/// <summary>
/// A <see cref="SyntaxNode"/> for the set position prefab.
/// </summary>
public sealed class SetPositionStatementSyntax : StatementSyntax
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SetPositionStatementSyntax"/> class.
    /// </summary>
    /// <param name="prefabId">Id of the prefab this node represents.</param>
    /// <param name="position">Position of the prefab this node represents.</param>
    /// <param name="outVoidConnections">Output void connections from this node.</param>
    /// <param name="objectTerminal">The object terminal; or <see langword="null"/>, if it is not connected.</param>
    /// <param name="positionTerminal">The position terminal; or <see langword="null"/>, if it is not connected.</param>
    /// <param name="rotationTerminal">The rotation terminal; or <see langword="null"/>, if it is not connected.</param>
    public SetPositionStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? objectTerminal, SyntaxTerminal? positionTerminal, SyntaxTerminal? rotationTerminal)
        : base(prefabId, position, outVoidConnections)
    {
        ObjectTerminal = objectTerminal;
        PositionTerminal = positionTerminal;
        RotationTerminal = rotationTerminal;
    }

    /// <summary>
    /// Gets the object terminal.
    /// </summary>
    /// <value>The object terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? ObjectTerminal { get; }

    /// <summary>
    /// Gets the position terminal.
    /// </summary>
    /// <value>The position terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? PositionTerminal { get; }

    /// <summary>
    /// Gets the rotation terminal.
    /// </summary>
    /// <value>The rotation terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? RotationTerminal { get; }

    /// <inheritdoc/>
    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(3)];
}
