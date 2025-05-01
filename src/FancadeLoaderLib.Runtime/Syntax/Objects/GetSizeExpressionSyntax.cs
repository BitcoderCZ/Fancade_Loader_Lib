using MathUtils.Vectors;

namespace FancadeLoaderLib.Runtime.Syntax.Objects;

public sealed class GetSizeExpressionSyntax : SyntaxNode
{
    public GetSizeExpressionSyntax(ushort3 position, SyntaxTerminal? @object)
        : base(489, position)
    {
        Object = @object;
    }

    public SyntaxTerminal? Object { get; }
}
