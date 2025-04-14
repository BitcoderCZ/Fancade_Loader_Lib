using FancadeLoaderLib.Editing;
using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace FancadeLoaderLib.Runtime.Syntax.Control;

public sealed class LateUpdateStatementSyntax : StatementSyntax
{
    public LateUpdateStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections)
        : base(prefabId, position, outVoidConnections)
    {
    }

    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(2)];
}
