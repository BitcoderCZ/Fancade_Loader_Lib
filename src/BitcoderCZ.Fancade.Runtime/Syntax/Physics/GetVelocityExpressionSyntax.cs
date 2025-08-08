using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Physics;

/// <summary>
/// A <see cref="SyntaxNode"/> for the get velocity prefab.
/// </summary>
public sealed class GetVelocityExpressionSyntax : SyntaxNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetVelocityExpressionSyntax"/> class.
    /// </summary>
    /// <param name="prefabId">Id of the prefab this node represents.</param>
    /// <param name="position">Position of the prefab this node represents.</param>
    /// <param name="object">The object terminal; or <see langword="null"/>, if it is not connected.</param>
    public GetVelocityExpressionSyntax(ushort prefabId, ushort3 position, SyntaxTerminal? @object)
        : base(prefabId, position)
    {
        Object = @object;
    }

    /// <summary>
    /// Gets the object terminal.
    /// </summary>
    /// <value>The object terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? Object { get; }
}
