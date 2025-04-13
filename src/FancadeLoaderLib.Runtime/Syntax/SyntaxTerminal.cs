using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Text;

namespace FancadeLoaderLib.Runtime.Syntax;

public sealed class SyntaxTerminal
{
    public SyntaxTerminal(SyntaxNode node, byte3 terminalPosition)
    {
        Node = node;
        TerminalPosition = terminalPosition;
    }

    public SyntaxNode Node { get; }

    public byte3 TerminalPosition { get; }
}
