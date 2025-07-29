using BitcoderCZ.Fancade;
using BitcoderCZ.Fancade.Editing;
using MathUtils.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Physics;

public sealed class LinearLimitsStatementSyntax : StatementSyntax
{
    public LinearLimitsStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? constraint, SyntaxTerminal? lower, SyntaxTerminal? upper)
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
