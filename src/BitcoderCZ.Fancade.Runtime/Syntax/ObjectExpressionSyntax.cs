using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade.Runtime.Syntax;

public sealed class ObjectExpressionSyntax : SyntaxNode
{
    public ObjectExpressionSyntax(ushort prefabId, ushort3 position)
        : base(prefabId, position)
    {
    }
}
