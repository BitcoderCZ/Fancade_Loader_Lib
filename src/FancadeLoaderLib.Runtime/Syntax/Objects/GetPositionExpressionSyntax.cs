using MathUtils.Vectors;

namespace FancadeLoaderLib.Runtime.Syntax.Objects;

public sealed class GetPositionExpressionSyntax : SyntaxNode
{
    public GetPositionExpressionSyntax(ushort3 position, SyntaxTerminal? @object)
        : base(278, position)
    {
        Object = @object;
    }

    public SyntaxTerminal? Object { get; }
}
