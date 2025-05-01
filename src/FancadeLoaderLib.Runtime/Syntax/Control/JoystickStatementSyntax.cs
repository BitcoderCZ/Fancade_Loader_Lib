using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Editing.Scripting.Settings;
using MathUtils.Vectors;
using System.Collections.Immutable;

namespace FancadeLoaderLib.Runtime.Syntax.Control;

public sealed class JoystickStatementSyntax : StatementSyntax
{
    public JoystickStatementSyntax(ushort3 position, ImmutableArray<Connection> outVoidConnections, JoystickType type)
        : base(592, position, outVoidConnections)
    {
        Type = type;
    }

    public JoystickType Type { get; }

    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(2)];
}
