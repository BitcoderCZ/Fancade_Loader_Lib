using FancadeLoaderLib.Editing;
using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace FancadeLoaderLib.Runtime.Syntax.Control;

public sealed class IfStatementSyntax : StatementSyntax
{
    public IfStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? condition)
        : base(prefabId, position, outVoidConnections)
    {
        Condition = condition;
    }

    public SyntaxTerminal? Condition { get; }

    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(2)];
}
