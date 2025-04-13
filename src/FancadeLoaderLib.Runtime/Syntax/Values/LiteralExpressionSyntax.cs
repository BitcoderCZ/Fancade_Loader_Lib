using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Text;

namespace FancadeLoaderLib.Runtime.Syntax.Values;

public sealed class LiteralExpressionSyntax : SyntaxNode
{
    public LiteralExpressionSyntax(ushort prefabId, ushort3 position, SignalType type, RuntimeValue value)
        : base(prefabId, position)
    {
        Type = type;
        Value = value;
    }

    public SignalType Type { get; }

    public RuntimeValue Value { get; }
}
