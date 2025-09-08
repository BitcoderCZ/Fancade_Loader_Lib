using BitcoderCZ.Maths.Vectors;
using static BitcoderCZ.Fancade.Utils.ThrowHelper;

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
    public ListExpressionSyntax(ushort prefabId, int3 position, SyntaxTerminal? variable, SyntaxTerminal? index)
        : base(prefabId, position)
    {
        if (prefabId is not (82 or 461 or 465 or 469 or 86 or 473))
        {
            ThrowArgumentOutOfRangeException(nameof(prefabId), $"{nameof(prefabId)} must be 82 or 461 or 465 or 469 or 86 or 473.");
        }

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
