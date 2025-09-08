using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Game;

/// <summary>
/// A <see cref="SyntaxNode"/> for the accelerometer prefab.
/// </summary>
public sealed class AccelerometerExpressionSyntax : SyntaxNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AccelerometerExpressionSyntax"/> class.
    /// </summary>
    /// <param name="position">Position of the prefab this node represents.</param>
    public AccelerometerExpressionSyntax(int3 position)
        : base(224, position)
    {
    }
}
