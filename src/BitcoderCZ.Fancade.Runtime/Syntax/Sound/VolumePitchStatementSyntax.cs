using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Sound;

public sealed class VolumePitchStatementSyntax : StatementSyntax
{
    public VolumePitchStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? channel, SyntaxTerminal? volume, SyntaxTerminal? pitch)
        : base(prefabId, position, outVoidConnections)
    {
        Channel = channel;
        Volume = volume;
        Pitch = pitch;
    }

    public SyntaxTerminal? Channel { get; }

    public SyntaxTerminal? Volume { get; }

    public SyntaxTerminal? Pitch { get; }

    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(3)];
}
