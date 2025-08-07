using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade.Runtime.Syntax;

/// <summary>
/// Represents a connection to the outer ast/prefab.
/// </summary>
public sealed class OuterExpressionSyntax : SyntaxNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OuterExpressionSyntax"/> class.
    /// </summary>
    /// <param name="prefabId">Id of the prefab.</param>
    public OuterExpressionSyntax(ushort prefabId)
        : base(prefabId, ushort3.One * Connection.IsFromToOutsideValue)
    {
    }
}
