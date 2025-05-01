using MathUtils.Vectors;
using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib.Runtime.Syntax.Math;

public sealed class MakeVecRotExpressionSyntax : SyntaxNode
{
    public MakeVecRotExpressionSyntax(ushort prefabId, ushort3 position, SyntaxTerminal? x, SyntaxTerminal? y, SyntaxTerminal? z)
        : base(prefabId, position)
    {
        if (prefabId is not (150 or 162))
        {
            ThrowArgumentOutOfRangeException(nameof(prefabId), $"{nameof(prefabId)} must be 150 or 162.");
        }

        X = x;
        Y = y;
        Z = z;
    }

    public SyntaxTerminal? X { get; }

    public SyntaxTerminal? Y { get; }

    public SyntaxTerminal? Z { get; }
}
