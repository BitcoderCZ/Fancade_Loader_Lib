using FancadeLoaderLib.Editing;
using MathUtils.Vectors;
using System.Collections.Immutable;

namespace FancadeLoaderLib.Runtime.Syntax.Control;

public sealed class LoopStatementSyntax : StatementSyntax
{
    public LoopStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? start, SyntaxTerminal? stop)
        : base(prefabId, position, outVoidConnections)
    {
        Start = start;
        Stop = stop;
    }

    public SyntaxTerminal? Start { get; }

    public SyntaxTerminal? Stop { get; }

    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(2)];
}
