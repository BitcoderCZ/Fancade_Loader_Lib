using MathUtils.Vectors;

namespace FancadeLoaderLib.Runtime.Syntax.Math;

public sealed class ScreenToWorldExpressionSyntax : SyntaxNode
{
    public ScreenToWorldExpressionSyntax(ushort prefabId, ushort3 position, SyntaxTerminal? screenX, SyntaxTerminal? screenY)
        : base(prefabId, position)
    {
        ScreenX = screenX;
        ScreenY = screenY;
    }

    public SyntaxTerminal? ScreenX { get; }

    public SyntaxTerminal? ScreenY { get; }
}
