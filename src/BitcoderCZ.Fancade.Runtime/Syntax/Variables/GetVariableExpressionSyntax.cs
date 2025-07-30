using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Variables;

public sealed class GetVariableExpressionSyntax : SyntaxNode
{
    public GetVariableExpressionSyntax(ushort prefabId, ushort3 position, Variable variable)
        : base(prefabId, position)
    {
        Variable = variable;
    }

    public Variable Variable { get; }
}
