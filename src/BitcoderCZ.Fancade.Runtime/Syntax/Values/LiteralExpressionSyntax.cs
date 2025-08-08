using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Values;

/// <summary>
/// A <see cref="SyntaxNode"/> for any literal prefab prefab.
/// </summary>
public sealed class LiteralExpressionSyntax : SyntaxNode
{
    public LiteralExpressionSyntax(ushort prefabId, ushort3 position, SignalType type, RuntimeValue value)
        : base(prefabId, position)
    {
        Type = type;
        Value = value;
    }

    public SignalType Type { get; }

    public RuntimeValue Value { get; }
}
