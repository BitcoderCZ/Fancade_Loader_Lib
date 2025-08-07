using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade.Runtime.Syntax;

/// <summary>
/// Represents a prefab that an object wire is connected to.
/// </summary>
public sealed class ObjectExpressionSyntax : SyntaxNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectExpressionSyntax"/> class.
    /// </summary>
    /// <param name="prefabId">Id of the prefab.</param>
    /// <param name="position">Position of the prefab.</param>
    public ObjectExpressionSyntax(ushort prefabId, ushort3 position)
        : base(prefabId, position)
    {
    }
}
