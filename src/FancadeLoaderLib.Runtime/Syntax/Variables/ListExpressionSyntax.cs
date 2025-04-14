using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Text;

namespace FancadeLoaderLib.Runtime.Syntax.Variables;

public sealed class ListExpressionSyntax : SyntaxNode
{
    public ListExpressionSyntax(ushort prefabId, ushort3 position, SyntaxTerminal? variable, SyntaxTerminal? index)
        : base(prefabId, position)
    {
        Variable = variable;
        Index = index;
    }

    public SyntaxTerminal? Variable { get; }

    public SyntaxTerminal? Index { get; }
}
