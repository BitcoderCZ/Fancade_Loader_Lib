﻿using FancadeLoaderLib.Editing;
using MathUtils.Vectors;
using System.Collections.Immutable;

namespace FancadeLoaderLib.Runtime.Syntax.Variables;

public sealed class IncDecNumberStatementSyntax : StatementSyntax
{
    public IncDecNumberStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? variable)
        : base(prefabId, position, outVoidConnections)
    {
        Variable = variable;
    }

    public SyntaxTerminal? Variable { get; }

    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(1)];
}
