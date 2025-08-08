using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Fancade.Editing.Scripting.Settings;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Game;

/// <summary>
/// A <see cref="SyntaxNode"/> for the set score prefab.
/// </summary>
public sealed class SetScoreStatementSyntax : StatementSyntax
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SetScoreStatementSyntax"/> class.
    /// </summary>
    /// <param name="prefabId">Id of the prefab this node represents.</param>
    /// <param name="position">Position of the prefab this node represents.</param>
    /// <param name="outVoidConnections">Output void connections from this node.</param>
    /// <param name="score">The score terminal; or <see langword="null"/>, if it is not connected.</param>
    /// <param name="coins">The coins terminal; or <see langword="null"/>, if it is not connected.</param>
    /// <param name="ranking">The new ranking mode.</param>
    public SetScoreStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? score, SyntaxTerminal? coins, Ranking ranking)
        : base(prefabId, position, outVoidConnections)
    {
        Score = score;
        Coins = coins;
        Ranking = ranking;
    }

    /// <summary>
    /// Gets the score terminal.
    /// </summary>
    /// <value>The score terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? Score { get; }

    /// <summary>
    /// Gets the coins terminal.
    /// </summary>
    /// <value>The coins terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? Coins { get; }

    /// <summary>
    /// Gets the new ranking mode.
    /// </summary>
    /// <value>The new ranking mode.</value>
    public Ranking Ranking { get; }

    /// <inheritdoc/>
    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(2)];
}
