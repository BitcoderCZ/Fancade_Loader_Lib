using FancadeLoaderLib.Editing;
using MathUtils.Vectors;
using System.Collections.Immutable;
using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib.Runtime.Syntax.Variables;

public sealed class IncDecNumberStatementSyntax : StatementSyntax
{
    public IncDecNumberStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? variable)
        : base(prefabId, position, outVoidConnections)
    {
        if (prefabId is not (556 or 558))
        {
            ThrowArgumentOutOfRangeException(nameof(prefabId), $"{nameof(prefabId)} must be 556 or 558.");
        }

        Variable = variable;
    }

    public SyntaxTerminal? Variable { get; }

    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(1)];
}
