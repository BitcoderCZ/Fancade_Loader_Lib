using MathUtils.Vectors;
using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib.Runtime.Syntax.Values;

public sealed class LiteralExpressionSyntax : SyntaxNode
{
    public LiteralExpressionSyntax(ushort prefabId, ushort3 position, SignalType type, RuntimeValue value)
        : base(prefabId, position)
    {
        if (prefabId is not (36 or 38 or 42 or 449 or 451))
        {
            ThrowArgumentOutOfRangeException(nameof(prefabId), $"{nameof(prefabId)} must be 36 or 38 or 42 or 449 or 451.");
        }

        Type = type;
        Value = value;
    }

    public SignalType Type { get; }

    public RuntimeValue Value { get; }
}
