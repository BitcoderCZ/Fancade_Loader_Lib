using MathUtils.Vectors;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Math;

public sealed class LineVsPlaneExpressionSyntax : SyntaxNode
{
    public LineVsPlaneExpressionSyntax(ushort prefabId, ushort3 position, SyntaxTerminal? lineFrom, SyntaxTerminal? lineTo, SyntaxTerminal? planePoint, SyntaxTerminal? planeNormal)
        : base(prefabId, position)
    {
        LineFrom = lineFrom;
        LineTo = lineTo;
        PlanePoint = planePoint;
        PlaneNormal = planeNormal;
    }

    public SyntaxTerminal? LineFrom { get; }

    public SyntaxTerminal? LineTo { get; }

    public SyntaxTerminal? PlanePoint { get; }

    public SyntaxTerminal? PlaneNormal { get; }
}
