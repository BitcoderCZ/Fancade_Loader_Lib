using MathUtils.Vectors;

namespace FancadeLoaderLib.Runtime.Syntax.Game;

public sealed class CurrentFrameExpressionSyntax : SyntaxNode
{
    public CurrentFrameExpressionSyntax(ushort prefabId, ushort3 position)
        : base(prefabId, position)
    {
    }
}
