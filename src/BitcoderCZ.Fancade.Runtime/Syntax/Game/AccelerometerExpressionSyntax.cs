using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Game;

/// <summary>
/// A <see cref="SyntaxNode"/> for the accelerometer prefab.
/// </summary>
public sealed class AccelerometerExpressionSyntax : SyntaxNode
{
    public AccelerometerExpressionSyntax(ushort prefabId, ushort3 position)
        : base(prefabId, position)
    {
    }
}
