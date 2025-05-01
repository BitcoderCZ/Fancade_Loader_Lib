using FancadeLoaderLib.Editing;
using MathUtils.Vectors;
using System.Collections.Immutable;
using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib.Runtime.Syntax.Variables;

public sealed class SetVaribleStatementSyntax : StatementSyntax
{
    public SetVaribleStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, Variable variable, SyntaxTerminal? value)
        : base(prefabId, position, outVoidConnections)
    {
        if (prefabId is not (428 or 430 or 432 or 434 or 436 or 438))
        {
            ThrowArgumentOutOfRangeException(nameof(prefabId), $"{nameof(prefabId)} must be 428 or 430 or 432 or 434 or 436 or 438.");
        }

        Variable = variable;
        Value = value;
    }

    public Variable Variable { get; }

    public SyntaxTerminal? Value { get; }

    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(1)];
}
