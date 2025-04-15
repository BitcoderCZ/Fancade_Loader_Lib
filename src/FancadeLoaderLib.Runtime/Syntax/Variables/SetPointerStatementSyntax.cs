using FancadeLoaderLib.Editing;
using MathUtils.Vectors;
using System.Collections.Immutable;

namespace FancadeLoaderLib.Runtime.Syntax.Variables;

public sealed class SetPointerStatementSyntax : StatementSyntax
{
    public SetPointerStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? variable, SyntaxTerminal? value)
        : base(prefabId, position, outVoidConnections)
    {
        Variable = variable;
        Value = value;
    }

    public SyntaxTerminal? Variable { get; }

    public SyntaxTerminal? Value { get; }

    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(2)];
}
