using BitcoderCZ.Fancade;
using BitcoderCZ.Fancade.Editing;
using MathUtils.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Physics;

public sealed class SetLockedStatementSyntax : StatementSyntax
{
    public SetLockedStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? @object, SyntaxTerminal? positionTerminal, SyntaxTerminal? rotationTerminal)
        : base(prefabId, position, outVoidConnections)
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