using FancadeLoaderLib.Editing;
using MathUtils.Vectors;
using System.Collections.Immutable;

namespace FancadeLoaderLib.Runtime.Syntax.Objects;

public sealed class DestroyObjectStatementSyntax : StatementSyntax
{
    public DestroyObjectStatementSyntax(ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? @object)
        : base(320, position, outVoidConnections)
    {
        Object = @object;
    }

    public SyntaxTerminal? Object { get; }

    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(2)];
}
