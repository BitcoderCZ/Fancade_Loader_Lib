using FancadeLoaderLib.Editing;
using MathUtils.Vectors;
using System.Collections.Immutable;

namespace FancadeLoaderLib.Runtime.Syntax.Control;

public sealed class CollisionStatementSyntax : StatementSyntax
{
    public CollisionStatementSyntax(ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? firstObject)
        : base(401, position, outVoidConnections)
    {
        FirstObject = firstObject;
    }

    public SyntaxTerminal? FirstObject { get; }

    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(4)];
}
