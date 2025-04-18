using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Editing.Scripting.Settings;
using MathUtils.Vectors;
using System.Collections.Immutable;

namespace FancadeLoaderLib.Runtime.Syntax.Game;

public sealed class SetScoreStatementSyntax : StatementSyntax
{
    public SetScoreStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? score, SyntaxTerminal? coins, Ranking ranking)
        : base(prefabId, position, outVoidConnections)
    {
        Score = score;
        Coins = coins;
        Ranking = ranking;
    }

    public SyntaxTerminal? Score { get; }

    public SyntaxTerminal? Coins { get; }

    public Ranking Ranking { get; }

    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(2)];
}
