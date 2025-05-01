using FancadeLoaderLib.Editing;
using MathUtils.Vectors;
using System.Collections.Immutable;
using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib.Runtime.Syntax.Variables;

public sealed class SetPointerStatementSyntax : StatementSyntax
{
    public SetPointerStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? variable, SyntaxTerminal? value)
        : base(prefabId, position, outVoidConnections)
    {
        if (prefabId is not (58 or 62 or 66 or 70 or 74 or 78))
        {
            ThrowArgumentOutOfRangeException(nameof(prefabId), $"{nameof(prefabId)} must be 58 or 62 or 66 or 70 or 74 or 78.");
        }

        Variable = variable;
        Value = value;
    }

    public SyntaxTerminal? Variable { get; }

    public SyntaxTerminal? Value { get; }

    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(2)];
}
