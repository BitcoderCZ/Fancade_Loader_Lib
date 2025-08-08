using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Physics;

/// <summary>
/// A <see cref="SyntaxNode"/> for the angular motor prefab.
/// </summary>
public sealed class AngularMotorStatementSyntax : StatementSyntax
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AngularMotorStatementSyntax"/> class.
    /// </summary>
    /// <param name="prefabId">Id of the prefab this node represents.</param>
    /// <param name="position">Position of the prefab this node represents.</param>
    /// <param name="outVoidConnections">Output void connections from this node.</param>
    /// <param name="constraint">The constraint terminal; or <see langword="null"/>, if it is not connected.</param>
    /// <param name="speed">The speed terminal; or <see langword="null"/>, if it is not connected.</param>
    /// <param name="force">The force terminal; or <see langword="null"/>, if it is not connected.</param>
    public AngularMotorStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? constraint, SyntaxTerminal? speed, SyntaxTerminal? force)
        : base(prefabId, position, outVoidConnections)
    {
        Constraint = constraint;
        Speed = speed;
        Force = force;
    }

    /// <summary>
    /// Gets the constraint terminal.
    /// </summary>
    /// <value>The constraint terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? Constraint { get; }

    /// <summary>
    /// Gets the speed terminal.
    /// </summary>
    /// <value>The speed terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? Speed { get; }

    /// <summary>
    /// Gets the force terminal.
    /// </summary>
    /// <value>The force terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? Force { get; }

    /// <inheritdoc/>
    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(3)];
}
