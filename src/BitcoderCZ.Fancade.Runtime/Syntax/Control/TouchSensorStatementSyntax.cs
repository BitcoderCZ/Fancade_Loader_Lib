using BitcoderCZ.Fancade;
using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Fancade.Editing.Scripting.Settings;
using MathUtils.Vectors;
using System.Collections.Immutable;
using static BitcoderCZ.Fancade.Utils.ThrowHelper;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Control;

public sealed class TouchSensorStatementSyntax : StatementSyntax
{
    public TouchSensorStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, TouchState state, int fingerIndex)
        : base(prefabId, position, outVoidConnections)
    {
        if (fingerIndex < 0 || fingerIndex > 2)
        {
            ThrowArgumentOutOfRangeException($"{nameof(fingerIndex)} must be between 0 and 2.", nameof(fingerIndex));
        }

        State = state;
        FingerIndex = fingerIndex;
    }

    public TouchState State { get; }

    public int FingerIndex { get; }

    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(3)];
}
