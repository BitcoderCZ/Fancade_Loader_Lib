﻿using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Physics;

public sealed class SetGravityStatementSyntax : StatementSyntax
{
    public SetGravityStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? gravity)
        : base(prefabId, position, outVoidConnections)
    {
        Gravity = gravity;
    }

    public SyntaxTerminal? Gravity { get; }

    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(2)];
}
