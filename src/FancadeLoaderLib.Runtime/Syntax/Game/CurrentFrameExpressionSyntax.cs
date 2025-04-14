using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Text;

namespace FancadeLoaderLib.Runtime.Syntax.Game;

public sealed class CurrentFrameExpressionSyntax : SyntaxNode
{
    public CurrentFrameExpressionSyntax(ushort prefabId, ushort3 position) 
        : base(prefabId, position)
    {
    }
}
