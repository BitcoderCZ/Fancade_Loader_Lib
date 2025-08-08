using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Math;

/// <summary>
/// A <see cref="SyntaxNode"/> for the screen to world prefab.
/// </summary>
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
