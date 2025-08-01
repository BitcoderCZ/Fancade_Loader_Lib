﻿using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Fancade.Editing.Scripting.Settings;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Sound;

public sealed class PlaySoundStatementSyntax : StatementSyntax
{
    public PlaySoundStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? volume, SyntaxTerminal? pitch, FcSound sound)
        : base(prefabId, position, outVoidConnections)
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
