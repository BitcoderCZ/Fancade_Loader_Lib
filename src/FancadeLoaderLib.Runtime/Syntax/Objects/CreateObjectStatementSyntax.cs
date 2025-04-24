using FancadeLoaderLib.Editing;
using MathUtils.Vectors;
using System.Collections.Immutable;

namespace FancadeLoaderLib.Runtime.Syntax.Objects;

public sealed class CreateObjectStatementSyntax : StatementSyntax
{
    public CreateObjectStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? original)
        : base(prefabId, position, outVoidConnections)
    {
        Original = original;
    }

    public SyntaxTerminal? Original { get; }

    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(2)];
}
