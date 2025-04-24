using MathUtils.Vectors;

namespace FancadeLoaderLib.Runtime.Syntax.Game;

public sealed class AccelerometerExpressionSyntax : SyntaxNode
{
    public AccelerometerExpressionSyntax(ushort prefabId, ushort3 position)
        : base(prefabId, position)
    {
    }
}
