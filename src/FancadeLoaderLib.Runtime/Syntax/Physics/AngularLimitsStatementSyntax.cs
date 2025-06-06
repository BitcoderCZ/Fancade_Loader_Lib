﻿using FancadeLoaderLib.Editing;
using MathUtils.Vectors;
using System.Collections.Immutable;

namespace FancadeLoaderLib.Runtime.Syntax.Physics;

public sealed class AngularLimitsStatementSyntax : StatementSyntax
{
    public AngularLimitsStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? constraint, SyntaxTerminal? lower, SyntaxTerminal? upper)
        : base(prefabId, position, outVoidConnections)
    {
        Constraint = constraint;
        Lower = lower;
        Upper = upper;
    }

    public SyntaxTerminal? Constraint { get; }

    public SyntaxTerminal? Lower { get; }

    public SyntaxTerminal? Upper { get; }

    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(3)];
}
