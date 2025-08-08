using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Physics;

/// <summary>
/// A <see cref="SyntaxNode"/> for the angular limits prefab.
/// </summary>
public sealed class AngularLimitsStatementSyntax : StatementSyntax
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AngularLimitsStatementSyntax"/> class.
    /// </summary>
    /// <param name="prefabId">Id of the prefab this node represents.</param>
    /// <param name="position">Position of the prefab this node represents.</param>
    /// <param name="outVoidConnections">Output void connections from this node.</param>
    /// <param name="constraint">The constraint terminal; or <see langword="null"/>, if it is not connected.</param>
    /// <param name="lower">The lower terminal; or <see langword="null"/>, if it is not connected.</param>
    /// <param name="upper">The upper terminal; or <see langword="null"/>, if it is not connected.</param>
    public AngularLimitsStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? constraint, SyntaxTerminal? lower, SyntaxTerminal? upper)
        : base(prefabId, position, outVoidConnections)
    {
        Constraint = constraint;
        Lower = lower;
        Upper = upper;
    }

    /// <summary>
    /// Gets the constraint terminal.
    /// </summary>
    /// <value>The constraint terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? Constraint { get; }

    /// <summary>
    /// Gets the lower terminal.
    /// </summary>
    /// <value>The lower terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? Lower { get; }

    /// <summary>
    /// Gets the upper terminal.
    /// </summary>
    /// <value>The upper terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? Upper { get; }

    /// <inheritdoc/>
    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(3)];
}
