using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Text;

namespace FancadeLoaderLib.Runtime.Syntax.Variables;

public sealed class GetVariableExpressionSyntax : SyntaxNode
{
    public GetVariableExpressionSyntax(ushort prefabId, ushort3 position, Variable variable)
        : base(prefabId, position)
    {
        Variable = variable;
    }

    public Variable Variable { get; }
}
