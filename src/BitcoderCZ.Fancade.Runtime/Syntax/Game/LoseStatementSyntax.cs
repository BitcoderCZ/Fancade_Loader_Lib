﻿using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Game;

public sealed class LoseStatementSyntax : StatementSyntax
{
    public LoseStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, int delay)
        : base(prefabId, position, outVoidConnections)
    {
        Delay = delay;
    }

    public int Delay { get; }

    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(2)];
}
