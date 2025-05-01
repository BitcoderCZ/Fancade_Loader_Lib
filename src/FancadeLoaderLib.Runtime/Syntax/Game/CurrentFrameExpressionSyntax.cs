using MathUtils.Vectors;

namespace FancadeLoaderLib.Runtime.Syntax.Game;

public sealed class CurrentFrameExpressionSyntax : SyntaxNode
{
    public CurrentFrameExpressionSyntax(ushort3 position)
        : base(564, position)
    {
    }
}
