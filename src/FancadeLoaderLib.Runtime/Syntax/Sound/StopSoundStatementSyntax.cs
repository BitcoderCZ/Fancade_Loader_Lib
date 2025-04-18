using FancadeLoaderLib.Editing;
using MathUtils.Vectors;
using System.Collections.Immutable;

namespace FancadeLoaderLib.Runtime.Syntax.Sound;

public sealed class StopSoundStatementSyntax : StatementSyntax
{
    public StopSoundStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? channel)
        : base(prefabId, position, outVoidConnections)
    {
        Channel = channel;
    }

    public SyntaxTerminal? Channel { get; }

    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(2)];
}
