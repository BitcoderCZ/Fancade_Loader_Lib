using MathUtils.Vectors;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Math;

public sealed class MakeVecRotExpressionSyntax : SyntaxNode
{
    public MakeVecRotExpressionSyntax(ushort prefabId, ushort3 position, SyntaxTerminal? x, SyntaxTerminal? y, SyntaxTerminal? z)
        : base(prefabId, position)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public SyntaxTerminal? X { get; }

    public SyntaxTerminal? Y { get; }

    public SyntaxTerminal? Z { get; }
}
