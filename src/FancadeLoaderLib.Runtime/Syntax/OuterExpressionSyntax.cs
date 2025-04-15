using MathUtils.Vectors;

namespace FancadeLoaderLib.Runtime.Syntax;

public sealed class OuterExpressionSyntax : SyntaxNode
{
    public OuterExpressionSyntax(ushort prefabId, ushort3 position)
        : base(prefabId, position)
    {
    }
}
