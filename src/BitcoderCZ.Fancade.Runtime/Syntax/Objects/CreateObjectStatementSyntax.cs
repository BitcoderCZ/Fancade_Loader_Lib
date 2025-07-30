using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Objects;

public sealed class CreateObjectStatementSyntax : StatementSyntax
{
    public CreateObjectStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? original)
        : base(prefabId, position, outVoidConnections)
    {
        Original = original;
    }

    public SyntaxTerminal? Original { get; }

    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(2)];
}
