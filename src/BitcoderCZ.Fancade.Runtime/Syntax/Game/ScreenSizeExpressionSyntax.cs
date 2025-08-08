using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Game;

/// <summary>
/// A <see cref="SyntaxNode"/> for the screen size prefab.
/// </summary>
public sealed class ScreenSizeExpressionSyntax : SyntaxNode
{
    public ScreenSizeExpressionSyntax(ushort prefabId, ushort3 position)
        : base(prefabId, position)
    {
    }
}
