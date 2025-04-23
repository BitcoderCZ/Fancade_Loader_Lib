using FancadeLoaderLib.Editing;
using MathUtils.Vectors;
using System.Collections.Immutable;

namespace FancadeLoaderLib.Runtime.Syntax.Physics;

public sealed class AngularMotorStatementSyntax : StatementSyntax
{
    public AngularMotorStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? constraint, SyntaxTerminal? speed, SyntaxTerminal? force)
        : base(prefabId, position, outVoidConnections)
    {
        Constraint = constraint;
        Speed = speed;
        Force = force;
    }

    public SyntaxTerminal? Constraint { get; }

    public SyntaxTerminal? Speed { get; }

    public SyntaxTerminal? Force { get; }

    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(3)];
}
