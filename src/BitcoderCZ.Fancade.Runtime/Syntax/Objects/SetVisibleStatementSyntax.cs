using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Objects;

/// <summary>
/// A <see cref="SyntaxNode"/> for the set visible prefab.
/// </summary>
public sealed class SetVisibleStatementSyntax : StatementSyntax
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SetVisibleStatementSyntax"/> class.
    /// </summary>
    /// <param name="prefabId">Id of the prefab this node represents.</param>
    /// <param name="position">Position of the prefab this node represents.</param>
    /// <param name="outVoidConnections">Output void connections from this node.</param>
    /// <param name="object">The object terminal; or <see langword="null"/>, if it is not connected.</param>
    /// <param name="visible">The visible terminal; or <see langword="null"/>, if it is not connected.</param>
    public SetVisibleStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? @object, SyntaxTerminal? visible)
        : base(prefabId, position, outVoidConnections)
    {
        Object = @object;
        Visible = visible;
    }

    /// <summary>
    /// Gets the object terminal.
    /// </summary>
    /// <value>The object terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? Object { get; }

    /// <summary>
    /// Gets the visible terminal.
    /// </summary>
    /// <value>The visible terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? Visible { get; }

    /// <inheritdoc/>
    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(2)];
}
