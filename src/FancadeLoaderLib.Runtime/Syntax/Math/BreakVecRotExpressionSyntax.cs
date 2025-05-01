using MathUtils.Vectors;
using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib.Runtime.Syntax.Math;

public sealed class BreakVecRotExpressionSyntax : SyntaxNode
{
    public BreakVecRotExpressionSyntax(ushort prefabId, ushort3 position, SyntaxTerminal? vecRot)
        : base(prefabId, position)
    {
        if (prefabId is not (156 or 442))
        {
            ThrowArgumentOutOfRangeException(nameof(prefabId), $"{nameof(prefabId)} must be 156 or 442.");
        }

        VecRot = vecRot;
    }

    public SyntaxTerminal? VecRot { get; }
}
