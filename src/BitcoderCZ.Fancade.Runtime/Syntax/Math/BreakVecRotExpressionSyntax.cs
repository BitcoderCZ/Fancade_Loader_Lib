using MathUtils.Vectors;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Math;

public sealed class BreakVecRotExpressionSyntax : SyntaxNode
{
    public BreakVecRotExpressionSyntax(ushort prefabId, ushort3 position, SyntaxTerminal? vecRot)
        : base(prefabId, position)
    {
        VecRot = vecRot;
    }

    public SyntaxTerminal? VecRot { get; }
}
