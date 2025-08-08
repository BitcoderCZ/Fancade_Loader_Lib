using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Math;

/// <summary>
/// A <see cref="SyntaxNode"/> for the break vector/rotation prefab.
/// </summary>
public sealed class BreakVecRotExpressionSyntax : SyntaxNode
{
    public BreakVecRotExpressionSyntax(ushort prefabId, ushort3 position, SyntaxTerminal? vecRot)
        : base(prefabId, position)
    {
        VecRot = vecRot;
    }

    public SyntaxTerminal? VecRot { get; }
}
