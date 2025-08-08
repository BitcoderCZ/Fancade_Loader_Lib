using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Physics;

/// <summary>
/// A <see cref="SyntaxNode"/> for the add force prefab.
/// </summary>
public sealed class AddForceStatementSyntax : StatementSyntax
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddForceStatementSyntax"/> class.
    /// </summary>
    /// <param name="prefabId">Id of the prefab this node represents.</param>
    /// <param name="position">Position of the prefab this node represents.</param>
    /// <param name="outVoidConnections">Output void connections from this node.</param>
    /// <param name="object">The object terminal; or <see langword="null"/>, if it is not connected.</param>
    /// <param name="force">The force terminal; or <see langword="null"/>, if it is not connected.</param>
    /// <param name="applyAt">The applyAt terminal; or <see langword="null"/>, if it is not connected.</param>
    /// <param name="torque">The torque terminal; or <see langword="null"/>, if it is not connected.</param>
    public AddForceStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? @object, SyntaxTerminal? force, SyntaxTerminal? applyAt, SyntaxTerminal? torque)
        : base(prefabId, position, outVoidConnections)
    {
        Object = @object;
        Force = force;
        ApplyAt = applyAt;
        Torque = torque;
    }

    /// <summary>
    /// Gets the object terminal.
    /// </summary>
    /// <value>The object terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? Object { get; }

    /// <summary>
    /// Gets the force terminal.
    /// </summary>
    /// <value>The force terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? Force { get; }

    /// <summary>
    /// Gets the applyAt terminal.
    /// </summary>
    /// <value>The applyAt terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? ApplyAt { get; }

    /// <summary>
    /// Gets the torque terminal.
    /// </summary>
    /// <value>The torque terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? Torque { get; }

    /// <inheritdoc/>
    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(4)];
}
