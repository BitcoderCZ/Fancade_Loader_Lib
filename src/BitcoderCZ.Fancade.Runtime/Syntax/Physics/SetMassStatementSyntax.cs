using BitcoderCZ.Fancade;
using BitcoderCZ.Fancade.Editing;
using MathUtils.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Physics;

public sealed class SetMassStatementSyntax : StatementSyntax
{
    public SetMassStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? @object, SyntaxTerminal? mass)
        : base(prefabId, position, outVoidConnections)
    {
        Object = @object;
        Mass = mass;
    }

    public SyntaxTerminal? Object { get; }

    public SyntaxTerminal? Mass { get; }

    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(2)];
}
