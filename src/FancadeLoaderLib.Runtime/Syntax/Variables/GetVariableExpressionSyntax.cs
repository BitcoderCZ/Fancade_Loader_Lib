using FancadeLoaderLib.Editing;
using MathUtils.Vectors;
using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib.Runtime.Syntax.Variables;

public sealed class GetVariableExpressionSyntax : SyntaxNode
{
    public GetVariableExpressionSyntax(ushort prefabId, ushort3 position, Variable variable)
        : base(prefabId, position)
    {
        if (prefabId is not (46 or 48 or 50 or 52 or 54 or 56))
        {
            ThrowArgumentOutOfRangeException(nameof(prefabId), $"{nameof(prefabId)} must be 46 or 48 or 50 or 52 or 54 or 56.");
        }

        Variable = variable;
    }

    public Variable Variable { get; }
}
