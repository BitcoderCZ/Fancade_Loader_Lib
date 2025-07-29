using BitcoderCZ.Fancade;
using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Fancade.Editing.Scripting.Settings;
using MathUtils.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Control;

public sealed class JoystickStatementSyntax : StatementSyntax
{
    public JoystickStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, JoystickType type)
        : base(prefabId, position, outVoidConnections)
    {
        Type = type;
    }

    public JoystickType Type { get; }

    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(2)];
}
