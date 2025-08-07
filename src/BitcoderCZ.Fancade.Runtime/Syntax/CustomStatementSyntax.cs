using BitcoderCZ.Maths.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax;

/// <summary>
/// Represents a non-stock (player created) prefab.
/// </summary>
public sealed class CustomStatementSyntax : StatementSyntax
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CustomStatementSyntax"/> class.
    /// </summary>
    /// <param name="prefabId">Id of the prefab.</param>
    /// <param name="position">Position of the prefab.</param>
    /// <param name="outVoidConnections">Output void connections from this node.</param>
    /// <param name="ast"><see cref="FcAST"/> representing the prefab.</param>
    /// <param name="connectedInputTerminals">Input connections to this node.</param>
    public CustomStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, FcAST ast, ImmutableArray<(byte3 TerminalPosition, SyntaxTerminal? ConnectedTerminal)> connectedInputTerminals)
        : base(prefabId, position, outVoidConnections)
    {
        AST = ast;
        ConnectedInputTerminals = connectedInputTerminals;
    }

    /// <summary>
    /// Gets the <see cref="FcAST"/> representing the prefab.
    /// </summary>
    /// <value><see cref="FcAST"/> representing the prefab.</value>
    public FcAST AST { get; }

    /// <summary>
    /// Gets the input connections to this node.
    /// </summary>
    /// <value>Input connections to this node.</value>
    public ImmutableArray<(byte3 TerminalPosition, SyntaxTerminal? ConnectedTerminal)> ConnectedInputTerminals { get; }

    /// <inheritdoc/>
    public override IEnumerable<byte3> InputVoidTerminals => AST.VoidInputs.Select(con => con.OutsideTerminal);
}
