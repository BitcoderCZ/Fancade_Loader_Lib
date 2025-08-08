using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Math;

/// <summary>
/// A <see cref="SyntaxNode"/> for the world to screen prefab.
/// </summary>
public sealed class WorldToScreenExpressionSyntax : SyntaxNode
{
    public WorldToScreenExpressionSyntax(ushort prefabId, ushort3 position, SyntaxTerminal? worldPos)
        : base(prefabId, position)
    {
        WorldPos = worldPos;
    }

    public SyntaxTerminal? WorldPos { get; }
}
