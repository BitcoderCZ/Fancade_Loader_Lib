using MathUtils.Vectors;

namespace FancadeLoaderLib.Runtime.Syntax.Game;

public sealed class ScreenSizeExpressionSyntax : SyntaxNode
{
    public ScreenSizeExpressionSyntax(ushort3 position)
        : base(220, position)
    {
    }
}
