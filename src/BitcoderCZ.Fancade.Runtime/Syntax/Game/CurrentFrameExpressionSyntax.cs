using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Game;

public sealed class CurrentFrameExpressionSyntax : SyntaxNode
{
    public CurrentFrameExpressionSyntax(ushort prefabId, ushort3 position)
        : base(prefabId, position)
    {
    }
}
