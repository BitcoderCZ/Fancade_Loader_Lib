using MathUtils.Vectors;

namespace FancadeLoaderLib.Runtime.Syntax.Math;

public sealed class WorldToScreenExpressionSyntax : SyntaxNode
{
    public WorldToScreenExpressionSyntax(ushort3 position, SyntaxTerminal? worldPos)
        : base(477, position)
    {
        WorldPos = worldPos;
    }

    public SyntaxTerminal? WorldPos { get; }
}
