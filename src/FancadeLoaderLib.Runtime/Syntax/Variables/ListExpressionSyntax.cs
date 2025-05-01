using MathUtils.Vectors;
using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib.Runtime.Syntax.Variables;

public sealed class ListExpressionSyntax : SyntaxNode
{
    public ListExpressionSyntax(ushort prefabId, ushort3 position, SyntaxTerminal? variable, SyntaxTerminal? index)
        : base(prefabId, position)
    {
        if (prefabId is not (82 or 461 or 465 or 469 or 86 or 473))
        {
            ThrowArgumentOutOfRangeException(nameof(prefabId), $"{nameof(prefabId)} must be 82 or 461 or 465 or 469 or 86 or 473.");
        }

        Variable = variable;
        Index = index;
    }

    public SyntaxTerminal? Variable { get; }

    public SyntaxTerminal? Index { get; }
}
