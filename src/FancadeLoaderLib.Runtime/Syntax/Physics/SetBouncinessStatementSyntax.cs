using FancadeLoaderLib.Editing;
using MathUtils.Vectors;
using System.Collections.Immutable;

namespace FancadeLoaderLib.Runtime.Syntax.Physics;

public sealed class SetBouncinessStatementSyntax : StatementSyntax
{
    public SetBouncinessStatementSyntax(ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? @object, SyntaxTerminal? bounciness)
        : base(336, position, outVoidConnections)
    {
        Object = @object;
        Bounciness = bounciness;
    }

    public SyntaxTerminal? Object { get; }

    public SyntaxTerminal? Bounciness { get; }

    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(2)];
}
