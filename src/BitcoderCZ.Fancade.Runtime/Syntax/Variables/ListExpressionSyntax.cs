using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Variables;

/// <summary>
/// A <see cref="SyntaxNode"/> for any list prefab.
/// </summary>
public sealed class ListExpressionSyntax : SyntaxNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListExpressionSyntax"/> class.
    /// </summary>
    /// <param name="prefabId">Id of the prefab this node represents.</param>
    /// <param name="position">Position of the prefab this node represents.</param>
    /// <param name="variable">The variable terminal; or <see langword="null"/>, if it is not connected.</param>
    /// <param name="index">The index terminal; or <see langword="null"/>, if it is not connected.</param>
    public ListExpressionSyntax(ushort prefabId, ushort3 position, SyntaxTerminal? variable, SyntaxTerminal? index)
        : base(prefabId, position)
    {
        Variable = variable;
        Index = index;
    }

    /// <summary>
    /// Gets the variable terminal.
    /// </summary>
    /// <value>The variable terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? Variable { get; }

    /// <summary>
    /// Gets the index terminal.
    /// </summary>
    /// <value>The index terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? Index { get; }
}
