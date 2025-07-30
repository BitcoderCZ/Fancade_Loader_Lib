using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Physics;

public sealed class AngularSpringStatementSyntax : StatementSyntax
{
    public AngularSpringStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? constraint, SyntaxTerminal? stiffness, SyntaxTerminal? damping)
        : base(prefabId, position, outVoidConnections)
    {
        Constraint = constraint;
        Stiffness = stiffness;
        Damping = damping;
    }

    public SyntaxTerminal? Constraint { get; }

    public SyntaxTerminal? Stiffness { get; }

    public SyntaxTerminal? Damping { get; }

    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(3)];
}
