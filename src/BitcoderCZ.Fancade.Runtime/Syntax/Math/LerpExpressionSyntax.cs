using MathUtils.Vectors;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Math;

public sealed class LerpExpressionSyntax : SyntaxNode
{
    public LerpExpressionSyntax(ushort prefabId, ushort3 position, SyntaxTerminal? from, SyntaxTerminal? to, SyntaxTerminal? amount)
        : base(prefabId, position)
    {
        From = from;
        To = to;
        Amount = amount;
    }

    public SyntaxTerminal? From { get; }

    public SyntaxTerminal? To { get; }

    public SyntaxTerminal? Amount { get; }
}
