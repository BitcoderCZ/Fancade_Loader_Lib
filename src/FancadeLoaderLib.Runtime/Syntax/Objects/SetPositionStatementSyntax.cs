using FancadeLoaderLib.Editing;
using MathUtils.Vectors;
using System.Collections.Immutable;

namespace FancadeLoaderLib.Runtime.Syntax.Objects;

public sealed class SetPositionStatementSyntax : StatementSyntax
{
    public SetPositionStatementSyntax(ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? objectTerminal, SyntaxTerminal? positionTerminal, SyntaxTerminal? rotationTerminal)
        : base(282, position, outVoidConnections)
    {
        ObjectTerminal = objectTerminal;
        PositionTerminal = positionTerminal;
        RotationTerminal = rotationTerminal;
    }

    public SyntaxTerminal? ObjectTerminal { get; }

    public SyntaxTerminal? PositionTerminal { get; }

    public SyntaxTerminal? RotationTerminal { get; }

    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(3)];
}
