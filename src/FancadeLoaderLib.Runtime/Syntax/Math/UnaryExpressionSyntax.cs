using MathUtils.Vectors;

namespace FancadeLoaderLib.Runtime.Syntax.Math;

// the opeation is determined by PrefabId
public sealed class UnaryExpressionSyntax : SyntaxNode
{
    public UnaryExpressionSyntax(ushort prefabId, ushort3 position, SyntaxTerminal? input)
        : base(prefabId, position)
    {
        Input = input;
    }

    public SyntaxTerminal? Input { get; }
}
