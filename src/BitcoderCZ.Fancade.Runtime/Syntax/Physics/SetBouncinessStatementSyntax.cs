using BitcoderCZ.Fancade;
using BitcoderCZ.Fancade.Editing;
using MathUtils.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Physics;

public sealed class SetBouncinessStatementSyntax : StatementSyntax
{
    public SetBouncinessStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? @object, SyntaxTerminal? bounciness)
        : base(prefabId, position, outVoidConnections)
    {
        Object = @object;
        Bounciness = bounciness;
    }

    public SyntaxTerminal? Object { get; }

    public SyntaxTerminal? Bounciness { get; }

    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(2)];
}
