using FancadeLoaderLib.Editing;
using MathUtils.Vectors;
using System.Collections.Immutable;

namespace FancadeLoaderLib.Runtime.Syntax.Physics;

public sealed class SetMassStatementSyntax : StatementSyntax
{
    public SetMassStatementSyntax(ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? @object, SyntaxTerminal? mass)
        : base(328, position, outVoidConnections)
    {
        Object = @object;
        Mass = mass;
    }

    public SyntaxTerminal? Object { get; }

    public SyntaxTerminal? Mass { get; }

    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(2)];
}
