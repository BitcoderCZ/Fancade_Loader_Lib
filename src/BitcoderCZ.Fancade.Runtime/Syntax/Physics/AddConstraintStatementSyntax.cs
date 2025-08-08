using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Physics;

/// <summary>
/// A <see cref="SyntaxNode"/> for the add constraint prefab.
/// </summary>
public sealed class AddConstraintStatementSyntax : StatementSyntax
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddConstraintStatementSyntax"/> class.
    /// </summary>
    /// <param name="prefabId">Id of the prefab this node represents.</param>
    /// <param name="position">Position of the prefab this node represents.</param>
    /// <param name="outVoidConnections">Output void connections from this node.</param>
    /// <param name="base">The base terminal; or <see langword="null"/>, if it is not connected.</param>
    /// <param name="part">The part terminal; or <see langword="null"/>, if it is not connected.</param>
    /// <param name="pivot">The pivot terminal; or <see langword="null"/>, if it is not connected.</param>
    public AddConstraintStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? @base, SyntaxTerminal? part, SyntaxTerminal? pivot)
        : base(prefabId, position, outVoidConnections)
    {
        Base = @base;
        Part = part;
        Pivot = pivot;
    }

    /// <summary>
    /// Gets the base terminal.
    /// </summary>
    /// <value>The base terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? Base { get; }

    /// <summary>
    /// Gets the value terminal.
    /// </summary>
    /// <value>The value terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? Part { get; }

    /// <summary>
    /// Gets the pivot terminal.
    /// </summary>
    /// <value>The pivot terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? Pivot { get; }

    /// <inheritdoc/>
    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(3)];
}
