using MathUtils.Vectors;
using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib.Runtime.Syntax.Math;

// the opeation is determined by PrefabId
public sealed class BinaryExpressionSyntax : SyntaxNode
{
    public BinaryExpressionSyntax(ushort prefabId, ushort3 position, SyntaxTerminal? input1, SyntaxTerminal? input2)
        : base(prefabId, position)
    {
        if (prefabId is not (92 or 96 or 100 or 104 or 108 or 112 or 116 or 120 or 124 or 172 or 457 or 132 or 136 or 140 or 421 or 146 or 417 or 128 or 481 or 168 or 176 or 180 or 580 or 570 or 574 or 190 or 200 or 204))
        {
            ThrowArgumentOutOfRangeException(nameof(prefabId), $"{nameof(prefabId)} must be 92 or 96 or 100 or 104 or 108 or 112 or 116 or 120 or 124 or 172 or 457 or 132 or 136 or 140 or 421 or 146 or 417 or 128 or 481 or 168 or 176 or 180 or 580 or 570 or 574 or 190 or 200 or 204.");
        }

        Input1 = input1;
        Input2 = input2;
    }

    public SyntaxTerminal? Input1 { get; }

    public SyntaxTerminal? Input2 { get; }
}
