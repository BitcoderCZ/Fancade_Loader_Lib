using BitcoderCZ.Maths.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax;

/// <summary>
/// A base class for all statement nodes.
/// </summary>
public abstract class StatementSyntax : SyntaxNode
{
    private protected StatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections)
        : base(prefabId, position)
    {
        OutVoidConnections = outVoidConnections;
    }

    /// <summary>
    /// Gets output void connections from this node.
    /// </summary>
    /// <value>Output void connections from this node.</value>
    public ImmutableArray<Connection> OutVoidConnections { get; }

    /// <summary>
    /// Gets positions of input void terminals of this node.
    /// </summary>
    /// <value>Positions of input void terminals of this node.</value>
    public abstract IEnumerable<byte3> InputVoidTerminals { get; }
}