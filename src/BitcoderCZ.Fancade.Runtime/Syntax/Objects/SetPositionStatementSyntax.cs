using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Objects;

public sealed class SetPositionStatementSyntax : StatementSyntax
{
    public SetPositionStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? objectTerminal, SyntaxTerminal? positionTerminal, SyntaxTerminal? rotationTerminal)
        : base(prefabId, position, outVoidConnections)
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
