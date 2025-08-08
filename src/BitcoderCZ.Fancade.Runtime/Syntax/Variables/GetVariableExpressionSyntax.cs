using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Variables;

/// <summary>
/// A <see cref="SyntaxNode"/> for any variable prefab.
/// </summary>
public sealed class GetVariableExpressionSyntax : SyntaxNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetVariableExpressionSyntax"/> class.
    /// </summary>
    /// <param name="prefabId">Id of the prefab this node represents.</param>
    /// <param name="position">Position of the prefab this node represents.</param>
    /// <param name="variable">The variable to get.</param>
    public GetVariableExpressionSyntax(ushort prefabId, ushort3 position, Variable variable)
        : base(prefabId, position)
    {
        Variable = variable;
    }

    /// <summary>
    /// Gets the variable to get.
    /// </summary>
    /// <value>The variable to get.</value>
    public Variable Variable { get; }
}
