using FancadeLoaderLib.Editing;
using MathUtils.Vectors;
using System.Collections.Immutable;

namespace FancadeLoaderLib.Runtime.Syntax.Math;

public sealed class RandomSeedStatementSyntax : StatementSyntax
{
    public RandomSeedStatementSyntax(ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? seed)
        : base(485, position, outVoidConnections)
    {
        Seed = seed;
    }

    public SyntaxTerminal? Seed { get; }

    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(2)];
}
