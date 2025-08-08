using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Game;

/// <summary>
/// A <see cref="SyntaxNode"/> for the current frame prefab.
/// </summary>
public sealed class CurrentFrameExpressionSyntax : SyntaxNode
{
    public CurrentFrameExpressionSyntax(ushort prefabId, ushort3 position)
        : base(prefabId, position)
    {
    }
}
