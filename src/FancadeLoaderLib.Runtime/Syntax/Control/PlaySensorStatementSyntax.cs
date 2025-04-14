using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Runtime.Syntax;
using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace FancadeLoaderLib.Runtime.Syntax.Control;

public sealed class PlaySensorStatementSyntax : StatementSyntax
{
    public PlaySensorStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections) 
        : base(prefabId, position, outVoidConnections)
    {
    }

    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(2)];
}
