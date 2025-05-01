using FancadeLoaderLib.Editing;
using MathUtils.Vectors;
using System.Collections.Immutable;

namespace FancadeLoaderLib.Runtime.Syntax.Physics;

public sealed class AddConstraintStatementSyntax : StatementSyntax
{
    public AddConstraintStatementSyntax(ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? @base, SyntaxTerminal? part, SyntaxTerminal? pivot)
        : base(340, position, outVoidConnections)
    {
        Base = @base;
        Part = part;
        Pivot = pivot;
    }

    public SyntaxTerminal? Base { get; }

    public SyntaxTerminal? Part { get; }

    public SyntaxTerminal? Pivot { get; }

    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(3)];
}
