using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Objects;

/// <summary>
/// A <see cref="SyntaxNode"/> for the get position prefab.
/// </summary>
public sealed class GetPositionExpressionSyntax : SyntaxNode
{
    public GetPositionExpressionSyntax(ushort prefabId, ushort3 position, SyntaxTerminal? @object)
        : base(prefabId, position)
    {
        Object = @object;
    }

    public SyntaxTerminal? Object { get; }
}
