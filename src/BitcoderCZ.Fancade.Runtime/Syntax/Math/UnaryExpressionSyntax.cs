using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Math;

// the opeation is determined by PrefabId

/// <summary>
/// A <see cref="SyntaxNode"/> for any unary math prefab.
/// </summary>
public sealed class UnaryExpressionSyntax : SyntaxNode
{
    public UnaryExpressionSyntax(ushort prefabId, ushort3 position, SyntaxTerminal? input)
        : base(prefabId, position)
    {
        Input = input;
    }

    public SyntaxTerminal? Input { get; }
}
