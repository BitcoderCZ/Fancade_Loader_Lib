﻿using MathUtils.Vectors;

namespace FancadeLoaderLib.Runtime.Syntax.Objects;

public sealed class GetSizeExpressionSyntax : SyntaxNode
{
    public GetSizeExpressionSyntax(ushort prefabId, ushort3 position, SyntaxTerminal? @object)
        : base(prefabId, position)
    {
        Object = @object;
    }

    public SyntaxTerminal? Object { get; }
}
