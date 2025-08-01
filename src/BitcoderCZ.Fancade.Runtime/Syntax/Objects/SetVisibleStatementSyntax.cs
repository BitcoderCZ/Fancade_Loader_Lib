﻿using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Objects;

public sealed class SetVisibleStatementSyntax : StatementSyntax
{
    public SetVisibleStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? @object, SyntaxTerminal? visible)
        : base(prefabId, position, outVoidConnections)
    {
        Object = @object;
        Visible = visible;
    }

    public SyntaxTerminal? Object { get; }

    public SyntaxTerminal? Visible { get; }

    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(2)];
}
