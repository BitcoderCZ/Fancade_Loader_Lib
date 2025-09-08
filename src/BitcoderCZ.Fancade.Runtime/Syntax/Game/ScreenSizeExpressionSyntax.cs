using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Game;

/// <summary>
/// A <see cref="SyntaxNode"/> for the screen size prefab.
/// </summary>
public sealed class ScreenSizeExpressionSyntax : SyntaxNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScreenSizeExpressionSyntax"/> class.
    /// </summary>
    /// <param name="position">Position of the prefab this node represents.</param>
    public ScreenSizeExpressionSyntax(int3 position)
        : base(220, position)
    {
    }
}
