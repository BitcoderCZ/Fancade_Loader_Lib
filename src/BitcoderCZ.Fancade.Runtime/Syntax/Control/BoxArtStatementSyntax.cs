using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Control;

/// <summary>
/// A <see cref="SyntaxNode"/> for the box art prefab.
/// </summary>
public sealed class BoxArtStatementSyntax : StatementSyntax
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BoxArtStatementSyntax"/> class.
    /// </summary>
    /// <inheritdoc cref="StatementSyntax(ushort, int3, ImmutableArray{Connection})"/>
    public BoxArtStatementSyntax(int3 position, ImmutableArray<Connection> outVoidConnections)
        : base(409, position, outVoidConnections)
    {
    }

    /// <inheritdoc/>
    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(2)];
}
