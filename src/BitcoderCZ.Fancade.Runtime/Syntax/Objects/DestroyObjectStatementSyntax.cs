using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Objects;

public sealed class DestroyObjectStatementSyntax : StatementSyntax
{
    public DestroyObjectStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? @object)
        : base(prefabId, position, outVoidConnections)
    {
        Object = @object;
    }

    public SyntaxTerminal? Object { get; }

    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(2)];
}
