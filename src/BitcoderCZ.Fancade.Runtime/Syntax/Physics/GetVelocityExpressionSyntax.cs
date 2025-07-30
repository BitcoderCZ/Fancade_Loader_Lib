using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Physics;

public sealed class GetVelocityExpressionSyntax : SyntaxNode
{
    public GetVelocityExpressionSyntax(ushort prefabId, ushort3 position, SyntaxTerminal? @object)
        : base(prefabId, position)
    {
        Object = @object;
    }

    public SyntaxTerminal? Object { get; }
}
