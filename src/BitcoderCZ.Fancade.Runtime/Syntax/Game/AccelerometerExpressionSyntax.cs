﻿using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Game;

public sealed class AccelerometerExpressionSyntax : SyntaxNode
{
    public AccelerometerExpressionSyntax(ushort prefabId, ushort3 position)
        : base(prefabId, position)
    {
    }
}
