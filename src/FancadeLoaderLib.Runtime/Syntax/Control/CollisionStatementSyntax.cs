using FancadeLoaderLib.Editing;
using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace FancadeLoaderLib.Runtime.Syntax.Control;

public sealed class CollisionStatementSyntax : StatementSyntax
{
    public CollisionStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? firstObject)
        : base(prefabId, position, outVoidConnections)
    {
        FirstObject = firstObject;
    }

    public SyntaxTerminal? FirstObject { get; }

    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(4)];
}
