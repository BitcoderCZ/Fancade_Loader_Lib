using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Physics;

public sealed class SetFrictionStatementSyntax : StatementSyntax
{
    public SetFrictionStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? @object, SyntaxTerminal? friction)
        : base(prefabId, position, outVoidConnections)
    {
        Object = @object;
        Friction = friction;
    }

    public SyntaxTerminal? Object { get; }

    public SyntaxTerminal? Friction { get; }

    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(2)];
}
