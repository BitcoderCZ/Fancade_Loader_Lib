using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Variables;

public sealed class SetVaribleStatementSyntax : StatementSyntax
{
    public SetVaribleStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, Variable variable, SyntaxTerminal? value)
        : base(prefabId, position, outVoidConnections)
    {
        Variable = variable;
        Value = value;
    }

    public Variable Variable { get; }

    public SyntaxTerminal? Value { get; }

    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(1)];
}
