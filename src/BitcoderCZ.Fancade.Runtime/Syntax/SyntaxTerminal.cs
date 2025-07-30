using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade.Runtime.Syntax;

public sealed class SyntaxTerminal
{
    public SyntaxTerminal(SyntaxNode node, byte3 position)
    {
        Node = node;
        Position = position;
    }

    public SyntaxNode Node { get; }

    public byte3 Position { get; }
}
