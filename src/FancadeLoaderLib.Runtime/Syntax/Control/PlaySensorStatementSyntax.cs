using FancadeLoaderLib.Editing;
using MathUtils.Vectors;
using System.Collections.Immutable;

namespace FancadeLoaderLib.Runtime.Syntax.Control;

public sealed class PlaySensorStatementSyntax : StatementSyntax
{
    public PlaySensorStatementSyntax(ushort3 position, ImmutableArray<Connection> outVoidConnections)
        : base(238, position, outVoidConnections)
    {
    }

    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(2)];
}
