﻿using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Math;

public sealed class RandomSeedStatementSyntax : StatementSyntax
{
    public RandomSeedStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? seed)
        : base(prefabId, position, outVoidConnections)
    {
        Seed = seed;
    }

    public SyntaxTerminal? Seed { get; }

    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(2)];
}
