using FancadeLoaderLib.Editing;
using MathUtils.Vectors;
using System.Collections.Immutable;

namespace FancadeLoaderLib.Runtime.Syntax.Physics;

public sealed class SetFrictionStatementSyntax : StatementSyntax
{
    public SetFrictionStatementSyntax(ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? @object, SyntaxTerminal? friction)
        : base(332, position, outVoidConnections)
    {
        Object = @object;
        Friction = friction;
    }

    public SyntaxTerminal? Object { get; }

    public SyntaxTerminal? Friction { get; }

    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(2)];
}
