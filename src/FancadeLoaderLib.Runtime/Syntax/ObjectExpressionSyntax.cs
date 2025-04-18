using MathUtils.Vectors;

namespace FancadeLoaderLib.Runtime.Syntax;

public sealed class ObjectExpressionSyntax : SyntaxNode
{
    public ObjectExpressionSyntax(ushort prefabId, ushort3 position)
        : base(prefabId, position)
    {
    }
}
