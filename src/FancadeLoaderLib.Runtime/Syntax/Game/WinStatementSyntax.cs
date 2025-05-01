using FancadeLoaderLib.Editing;
using MathUtils.Vectors;
using System.Collections.Immutable;

namespace FancadeLoaderLib.Runtime.Syntax.Game;

public sealed class WinStatementSyntax : StatementSyntax
{
    public WinStatementSyntax(ushort3 position, ImmutableArray<Connection> outVoidConnections, int delay)
        : base(252, position, outVoidConnections)
    {
        Delay = delay;
    }

    public int Delay { get; }

    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(2)];
}
