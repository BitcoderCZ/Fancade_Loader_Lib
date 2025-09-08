using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Objects;

/// <summary>
/// A <see cref="SyntaxNode"/> for the get position prefab.
/// </summary>
public sealed class GetPositionExpressionSyntax : SyntaxNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetPositionExpressionSyntax"/> class.
    /// </summary>
    /// <param name="position">Position of the prefab this node represents.</param>
    /// <param name="object">The object terminal; or <see langword="null"/>, if it is not connected.</param>
    public GetPositionExpressionSyntax(int3 position, SyntaxTerminal? @object)
        : base(278, position)
    {
        Object = @object;
    }

    /// <summary>
    /// Gets the object terminal.
    /// </summary>
    /// <value>The object terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? Object { get; }
}
