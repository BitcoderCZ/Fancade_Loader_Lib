using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Objects;

/// <summary>
/// A <see cref="SyntaxNode"/> for the raycast prefab.
/// </summary>
public sealed class RaycastExpressionSyntax : SyntaxNode
{
    public RaycastExpressionSyntax(ushort prefabId, ushort3 position, SyntaxTerminal? from, SyntaxTerminal? to)
        : base(prefabId, position)
    {
        From = from;
        To = to;
    }

    public SyntaxTerminal? From { get; }

    public SyntaxTerminal? To { get; }
}
