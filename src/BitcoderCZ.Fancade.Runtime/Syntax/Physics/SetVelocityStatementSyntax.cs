using BitcoderCZ.Fancade;
using BitcoderCZ.Fancade.Editing;
using MathUtils.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Physics;

public sealed class SetVelocityStatementSyntax : StatementSyntax
{
    public SetVelocityStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? @object, SyntaxTerminal? velocity, SyntaxTerminal? spin)
        : base(prefabId, position, outVoidConnections)
    {
        Object = @object;
        Velocity = velocity;
        Spin = spin;
    }

    public SyntaxTerminal? Object { get; }

    public SyntaxTerminal? Velocity { get; }

    public SyntaxTerminal? Spin { get; }

    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(3)];
}
