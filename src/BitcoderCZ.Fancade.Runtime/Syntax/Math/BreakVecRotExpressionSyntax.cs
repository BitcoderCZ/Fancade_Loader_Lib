using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Math;

/// <summary>
/// A <see cref="SyntaxNode"/> for the break vector/rotation prefab.
/// </summary>
public sealed class BreakVecRotExpressionSyntax : SyntaxNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BreakVecRotExpressionSyntax"/> class.
    /// </summary>
    /// <param name="prefabId">Id of the prefab this node represents.</param>
    /// <param name="position">Position of the prefab this node represents.</param>
    /// <param name="vecRot">The vector/rotation terminal; or <see langword="null"/>, if it is not connected.</param>
    public BreakVecRotExpressionSyntax(ushort prefabId, ushort3 position, SyntaxTerminal? vecRot)
        : base(prefabId, position)
    {
        VecRot = vecRot;
    }

    /// <summary>
    /// Gets the vector/rotation terminal.
    /// </summary>
    /// <value>The vector/rotation terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? VecRot { get; }
}
