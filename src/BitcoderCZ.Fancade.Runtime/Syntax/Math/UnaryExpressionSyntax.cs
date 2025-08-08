using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Math;

// the opeation is determined by PrefabId

/// <summary>
/// A <see cref="SyntaxNode"/> for any unary math prefab.
/// </summary>
public sealed class UnaryExpressionSyntax : SyntaxNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnaryExpressionSyntax"/> class.
    /// </summary>
    /// <param name="prefabId">Id of the prefab this node represents.</param>
    /// <param name="position">Position of the prefab this node represents.</param>
    /// <param name="input">The input terminal; or <see langword="null"/>, if it is not connected.</param>
    public UnaryExpressionSyntax(ushort prefabId, ushort3 position, SyntaxTerminal? input)
        : base(prefabId, position)
    {
        Input = input;
    }

    /// <summary>
    /// Gets the input terminal.
    /// </summary>
    /// <value>The input terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? Input { get; }
}
