using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Control;

public sealed class IfStatementSyntax : StatementSyntax
{
    public IfStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? condition)
        : base(prefabId, position, outVoidConnections)
    {
        Condition = condition;
    }

    public SyntaxTerminal? Condition { get; }

    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(2)];
}
