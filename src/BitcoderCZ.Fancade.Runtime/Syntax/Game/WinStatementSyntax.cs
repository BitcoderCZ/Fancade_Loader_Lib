using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Game;

/// <summary>
/// A <see cref="SyntaxNode"/> for the win prefab.
/// </summary>
public sealed class WinStatementSyntax : StatementSyntax
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WinStatementSyntax"/> class.
    /// </summary>
    /// <param name="position">Position of the prefab this node represents.</param>
    /// <param name="outVoidConnections">Output void connections from this node.</param>
    /// <param name="delay">Time, in frames, before the game is won.</param>
    public WinStatementSyntax(int3 position, ImmutableArray<Connection> outVoidConnections, int delay)
        : base(252, position, outVoidConnections)
    {
        Delay = delay;
    }

    /// <summary>
    /// Gets the time, in frames, before the game is won.
    /// </summary>
    /// <value>Time, in frames, before the game is won.</value>
    public int Delay { get; }

    /// <inheritdoc/>
    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(2)];
}
