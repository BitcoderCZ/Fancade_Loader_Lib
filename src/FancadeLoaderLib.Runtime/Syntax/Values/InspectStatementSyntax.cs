using FancadeLoaderLib.Editing;
using MathUtils.Vectors;
using System.Collections.Immutable;
using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib.Runtime.Syntax.Values;

public sealed class InspectStatementSyntax : StatementSyntax
{
    public InspectStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, SignalType type, SyntaxTerminal? input)
        : base(prefabId, position, outVoidConnections)
    {
        if (prefabId is not (16 or 20 or 24 or 28 or 32))
        {
            ThrowArgumentOutOfRangeException(nameof(prefabId), $"{nameof(prefabId)} must be 16 or 20 or 24 or 28 or 32.");
        }

        Type = type;
        Input = input;
    }

    public SignalType Type { get; }

    public SyntaxTerminal? Input { get; }

    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(2)];
}
