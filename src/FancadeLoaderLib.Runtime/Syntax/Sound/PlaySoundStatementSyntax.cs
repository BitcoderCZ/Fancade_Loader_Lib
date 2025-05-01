using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Editing.Scripting.Settings;
using MathUtils.Vectors;
using System.Collections.Immutable;

namespace FancadeLoaderLib.Runtime.Syntax.Sound;

public sealed class PlaySoundStatementSyntax : StatementSyntax
{
    public PlaySoundStatementSyntax(ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? volume, SyntaxTerminal? pitch, FcSound sound)
        : base(264, position, outVoidConnections)
    {
        Volume = volume;
        Pitch = pitch;
        Sound = sound;
    }

    public SyntaxTerminal? Volume { get; }

    public SyntaxTerminal? Pitch { get; }

    public FcSound Sound { get; }

    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(2)];
}
