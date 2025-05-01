using MathUtils.Vectors;

namespace FancadeLoaderLib.Runtime.Syntax.Game;

public sealed class AccelerometerExpressionSyntax : SyntaxNode
{
    public AccelerometerExpressionSyntax(ushort3 position)
        : base(224, position)
    {
    }
}
