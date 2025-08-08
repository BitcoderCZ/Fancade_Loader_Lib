using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Values;

/// <summary>
/// A <see cref="SyntaxNode"/> for the inspect prefab.
/// </summary>
public sealed class InspectStatementSyntax : StatementSyntax
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InspectStatementSyntax"/> class.
    /// </summary>
    /// <param name="prefabId">Id of the prefab this node represents.</param>
    /// <param name="position">Position of the prefab this node represents.</param>
    /// <param name="outVoidConnections">Output void connections from this node.</param>
    /// <param name="type">Type of the inspect block.</param>
    /// <param name="input">The input terminal; or <see langword="null"/>, if it is not connected.</param>
    public InspectStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, SignalType type, SyntaxTerminal? input)
        : base(prefabId, position, outVoidConnections)
    {
        Type = type;
        Input = input;
    }

    /// <summary>
    /// Gets the type of the inspect block.
    /// </summary>
    /// <value>Type of the inspect block.</value>
    public SignalType Type { get; }

    /// <summary>
    /// Gets the input terminal.
    /// </summary>
    /// <value>The input terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? Input { get; }

    /// <inheritdoc/>
    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(2)];
}
