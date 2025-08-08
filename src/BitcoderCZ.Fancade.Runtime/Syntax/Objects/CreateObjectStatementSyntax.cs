using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Objects;

/// <summary>
/// A <see cref="SyntaxNode"/> for the create object prefab.
/// </summary>
public sealed class CreateObjectStatementSyntax : StatementSyntax
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateObjectStatementSyntax"/> class.
    /// </summary>
    /// <param name="prefabId">Id of the prefab this node represents.</param>
    /// <param name="position">Position of the prefab this node represents.</param>
    /// <param name="outVoidConnections">Output void connections from this node.</param>
    /// <param name="original">The original terminal; or <see langword="null"/>, if it is not connected.</param>
    public CreateObjectStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? original)
        : base(prefabId, position, outVoidConnections)
    {
        Original = original;
    }

    /// <summary>
    /// Gets the original terminal.
    /// </summary>
    /// <value>The original terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? Original { get; }

    /// <inheritdoc/>
    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(2)];
}
