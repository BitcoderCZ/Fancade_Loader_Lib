using MathUtils.Vectors;

namespace FancadeLoaderLib.Runtime.Syntax.Math;

public sealed class BreakVecRotExpressionnSyntax : SyntaxNode
{
    public BreakVecRotExpressionnSyntax(ushort prefabId, ushort3 position, SyntaxTerminal? vecRot)
        : base(prefabId, position)
    {
        VecRot = vecRot;
    }

    public SyntaxTerminal? VecRot { get; }
}
