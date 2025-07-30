using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Control;

public sealed class PlaySensorStatementSyntax : StatementSyntax
{
    public PlaySensorStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections)
        : base(prefabId, position, outVoidConnections)
    {
    }

    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(2)];
}
