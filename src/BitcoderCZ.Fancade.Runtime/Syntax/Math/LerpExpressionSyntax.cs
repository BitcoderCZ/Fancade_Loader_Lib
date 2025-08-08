using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Math;

/// <summary>
/// A <see cref="SyntaxNode"/> for the lerp prefab.
/// </summary>
public sealed class LerpExpressionSyntax : SyntaxNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LerpExpressionSyntax"/> class.
    /// </summary>
    /// <param name="prefabId">Id of the prefab this node represents.</param>
    /// <param name="position">Position of the prefab this node represents.</param>
    /// <param name="from">The from terminal; or <see langword="null"/>, if it is not connected.</param>
    /// <param name="to">The to terminal; or <see langword="null"/>, if it is not connected.</param>
    /// <param name="amount">The amount terminal; or <see langword="null"/>, if it is not connected.</param>
    public LerpExpressionSyntax(ushort prefabId, ushort3 position, SyntaxTerminal? from, SyntaxTerminal? to, SyntaxTerminal? amount)
        : base(prefabId, position)
    {
        From = from;
        To = to;
        Amount = amount;
    }

    /// <summary>
    /// Gets the from terminal.
    /// </summary>
    /// <value>The from terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? From { get; }

    /// <summary>
    /// Gets the to terminal.
    /// </summary>
    /// <value>The to terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? To { get; }

    /// <summary>
    /// Gets the amount terminal.
    /// </summary>
    /// <value>The amount terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? Amount { get; }
}
