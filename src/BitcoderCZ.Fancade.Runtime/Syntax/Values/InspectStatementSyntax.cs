﻿using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Values;

public sealed class InspectStatementSyntax : StatementSyntax
{
    public InspectStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, SignalType type, SyntaxTerminal? input)
        : base(prefabId, position, outVoidConnections)
    {
        Type = type;
        Input = input;
    }

    public SignalType Type { get; }

    public SyntaxTerminal? Input { get; }

    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(2)];
}
