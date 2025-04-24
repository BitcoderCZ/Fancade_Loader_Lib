using FancadeLoaderLib.Editing;
using MathUtils.Vectors;
using System.Collections.Immutable;

namespace FancadeLoaderLib.Runtime.Syntax.Physics;

public sealed class SetGravityStatementSyntax : StatementSyntax
{
    public SetGravityStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? gravity)
        : base(prefabId, position, outVoidConnections)
    {
        Gravity = gravity;
    }

    public SyntaxTerminal? Gravity { get; }

    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(2)];
}
