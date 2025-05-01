using FancadeLoaderLib.Editing;
using MathUtils.Vectors;
using System.Collections.Immutable;

namespace FancadeLoaderLib.Runtime.Syntax.Physics;

public sealed class SetLockedStatementSyntax : StatementSyntax
{
    public SetLockedStatementSyntax(ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? @object, SyntaxTerminal? positionTerminal, SyntaxTerminal? rotationTerminal)
        : base(310, position, outVoidConnections)
    {
        Object = @object;
        PositionTerminal = positionTerminal;
        RotationTerminal = rotationTerminal;
    }

    public SyntaxTerminal? Object { get; }

    public SyntaxTerminal? PositionTerminal { get; }

    public SyntaxTerminal? RotationTerminal { get; }

    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(3)];
}