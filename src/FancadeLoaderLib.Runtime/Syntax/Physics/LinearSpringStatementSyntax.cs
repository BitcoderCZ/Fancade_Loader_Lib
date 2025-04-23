using FancadeLoaderLib.Editing;
using MathUtils.Vectors;
using System.Collections.Immutable;

namespace FancadeLoaderLib.Runtime.Syntax.Physics;

public sealed class LinearSpringStatementSyntax : StatementSyntax
{
    public LinearSpringStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? constraint, SyntaxTerminal? stiffness, SyntaxTerminal? damping)
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
