using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Physics;

/// <summary>
/// A <see cref="SyntaxNode"/> for the set locked prefab.
/// </summary>
public sealed class SetLockedStatementSyntax : StatementSyntax
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SetLockedStatementSyntax"/> class.
    /// </summary>
    /// <param name="prefabId">Id of the prefab this node represents.</param>
    /// <param name="position">Position of the prefab this node represents.</param>
    /// <param name="outVoidConnections">Output void connections from this node.</param>
    /// <param name="object">The object terminal; or <see langword="null"/>, if it is not connected.</param>
    /// <param name="positionTerminal">The position terminal; or <see langword="null"/>, if it is not connected.</param>
    /// <param name="rotationTerminal">The rotation terminal; or <see langword="null"/>, if it is not connected.</param>
    public SetLockedStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? @object, SyntaxTerminal? positionTerminal, SyntaxTerminal? rotationTerminal)
        : base(prefabId, position, outVoidConnections)
    {
        Object = @object;
        PositionTerminal = positionTerminal;
        RotationTerminal = rotationTerminal;
    }

    /// <summary>
    /// Gets the object terminal.
    /// </summary>
    /// <value>The object terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? Object { get; }

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