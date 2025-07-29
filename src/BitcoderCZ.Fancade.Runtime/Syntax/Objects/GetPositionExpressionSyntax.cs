using MathUtils.Vectors;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Objects;

public sealed class GetPositionExpressionSyntax : SyntaxNode
{
    public GetPositionExpressionSyntax(ushort prefabId, ushort3 position, SyntaxTerminal? @object)
        : base(prefabId, position)
    {
        Object = @object;
    }

    public SyntaxTerminal? Object { get; }
}
