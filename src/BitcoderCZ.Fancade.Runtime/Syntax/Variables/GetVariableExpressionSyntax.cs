using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Variables;

/// <summary>
/// A <see cref="SyntaxNode"/> for any variable prefab.
/// </summary>
public sealed class GetVariableExpressionSyntax : SyntaxNode
{
    public GetVariableExpressionSyntax(ushort prefabId, ushort3 position, Variable variable)
        : base(prefabId, position)
    {
        Variable = variable;
    }

    public Variable Variable { get; }
}
