using MathUtils.Vectors;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Game;

public sealed class ScreenSizeExpressionSyntax : SyntaxNode
{
    public ScreenSizeExpressionSyntax(ushort prefabId, ushort3 position)
        : base(prefabId, position)
    {
    }
}
