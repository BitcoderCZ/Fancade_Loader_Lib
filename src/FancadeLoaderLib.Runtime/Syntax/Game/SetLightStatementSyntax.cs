using FancadeLoaderLib.Editing;
using MathUtils.Vectors;
using System.Collections.Immutable;

namespace FancadeLoaderLib.Runtime.Syntax.Game;

public sealed class SetLightStatementSyntax : StatementSyntax
{
    public SetLightStatementSyntax(ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? positionTerminal, SyntaxTerminal? rotationTerminal)
        : base(274, position, outVoidConnections)
    {
        PositionTerminal = positionTerminal;
        RotationTerminal = rotationTerminal;
    }

    public SyntaxTerminal? PositionTerminal { get; }

    public SyntaxTerminal? RotationTerminal { get; }

    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(2)];
}
