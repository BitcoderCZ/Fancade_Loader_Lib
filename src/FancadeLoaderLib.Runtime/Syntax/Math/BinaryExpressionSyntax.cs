using MathUtils.Vectors;

namespace FancadeLoaderLib.Runtime.Syntax.Math;

// the opeation is determined by PrefabId
public sealed class BinaryExpressionSyntax : SyntaxNode
{
    public BinaryExpressionSyntax(ushort prefabId, ushort3 position, SyntaxTerminal? input1, SyntaxTerminal? input2)
        : base(prefabId, position)
    {
        Input1 = input1;
        Input2 = input2;
    }

    public SyntaxTerminal? Input1 { get; }

    public SyntaxTerminal? Input2 { get; }
}
