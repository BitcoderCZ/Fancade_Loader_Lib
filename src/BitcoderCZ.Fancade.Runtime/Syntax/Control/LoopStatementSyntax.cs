using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Control;

/// <summary>
/// A <see cref="SyntaxNode"/> for the loop prefab.
/// </summary>
public sealed class LoopStatementSyntax : StatementSyntax
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LoopStatementSyntax"/> class.
    /// </summary>
    /// <param name="prefabId">Id of the prefab this node represents.</param>
    /// <param name="position">Position of the prefab this node represents.</param>
    /// <param name="outVoidConnections">Output void connections from this node.</param>
    /// <param name="start">The start terminal; or <see langword="null"/>, if it is not connected.</param>
    /// <param name="stop">The stop terminal; or <see langword="null"/>, if it is not connected.</param>
    public LoopStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? start, SyntaxTerminal? stop)
        : base(prefabId, position, outVoidConnections)
    {
        Start = start;
        Stop = stop;
    }

    /// <summary>
    /// Gets the start terminal.
    /// </summary>
    /// <value>The start terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? Start { get; }

    /// <summary>
    /// Gets the stop terminal.
    /// </summary>
    /// <value>The stop terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? Stop { get; }

    /// <inheritdoc/>
    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(2)];
}
