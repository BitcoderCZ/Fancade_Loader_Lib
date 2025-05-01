using MathUtils.Vectors;
using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib.Runtime.Syntax.Math;

// the opeation is determined by PrefabId
public sealed class UnaryExpressionSyntax : SyntaxNode
{
    public UnaryExpressionSyntax(ushort prefabId, ushort3 position, SyntaxTerminal? input)
        : base(prefabId, position)
    {
        if (prefabId is not (90 or 144 or 440 or 413 or 453 or 184 or 186 or 188 or 455 or 578))
        {
            ThrowArgumentOutOfRangeException(nameof(prefabId), $"{nameof(prefabId)} must be 90 or 144 or 440 or 413 or 453 or 184 or 186 or 188 or 455 or 578.");
        }

        Input = input;
    }

    public SyntaxTerminal? Input { get; }
}
