using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Physics;

public sealed class AddForceStatementSyntax : StatementSyntax
{
    public AddForceStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? @object, SyntaxTerminal? force, SyntaxTerminal? applyAt, SyntaxTerminal? torque)
        : base(prefabId, position, outVoidConnections)
    {
        Object = @object;
        Force = force;
        ApplyAt = applyAt;
        Torque = torque;
    }

    public SyntaxTerminal? Object { get; }

    public SyntaxTerminal? Force { get; }

    public SyntaxTerminal? ApplyAt { get; }

    public SyntaxTerminal? Torque { get; }

    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(4)];
}
