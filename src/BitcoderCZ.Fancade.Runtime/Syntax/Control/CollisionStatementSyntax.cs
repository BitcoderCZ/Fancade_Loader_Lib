using BitcoderCZ.Fancade;
using BitcoderCZ.Fancade.Editing;
using MathUtils.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Control;

public sealed class CollisionStatementSyntax : StatementSyntax
{
    public CollisionStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? firstObject)
        : base(prefabId, position, outVoidConnections)
    {
        FirstObject = firstObject;
    }

    public SyntaxTerminal? FirstObject { get; }

    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(4)];
}
