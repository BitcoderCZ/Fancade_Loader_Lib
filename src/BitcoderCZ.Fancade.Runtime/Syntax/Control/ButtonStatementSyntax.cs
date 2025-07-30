using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Fancade.Editing.Scripting.Settings;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Control;

public sealed class ButtonStatementSyntax : StatementSyntax
{
    public ButtonStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, ButtonType type)
        : base(prefabId, position, outVoidConnections)
    {
        Type = type;
    }

    public ButtonType Type { get; }

    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(2)];
}
