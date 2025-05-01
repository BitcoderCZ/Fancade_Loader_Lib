using MathUtils.Vectors;

namespace FancadeLoaderLib.Runtime.Syntax.Physics;

public sealed class GetVelocityExpressionSyntax : SyntaxNode
{
    public GetVelocityExpressionSyntax(ushort3 position, SyntaxTerminal? @object)
        : base(288, position)
    {
        Object = @object;
    }

    public SyntaxTerminal? Object { get; }
}
