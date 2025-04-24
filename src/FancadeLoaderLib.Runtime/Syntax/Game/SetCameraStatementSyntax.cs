using FancadeLoaderLib.Editing;
using MathUtils.Vectors;
using System.Collections.Immutable;

namespace FancadeLoaderLib.Runtime.Syntax.Game;

public sealed class SetCameraStatementSyntax : StatementSyntax
{
    public SetCameraStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? positionTerminal, SyntaxTerminal? rotationTerminal, SyntaxTerminal? rangeTerminal, bool perspective)
        : base(prefabId, position, outVoidConnections)
    {
        PositionTerminal = positionTerminal;
        RotationTerminal = rotationTerminal;
        RangeTerminal = rangeTerminal;
        Perspective = perspective;
    }

    public SyntaxTerminal? PositionTerminal { get; }

    public SyntaxTerminal? RotationTerminal { get; }

    public SyntaxTerminal? RangeTerminal { get; }

    public bool Perspective { get; }

    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(3)];
}
