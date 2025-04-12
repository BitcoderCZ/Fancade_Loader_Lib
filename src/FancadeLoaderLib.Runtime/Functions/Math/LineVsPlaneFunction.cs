using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Runtime.Utils;
using MathUtils.Vectors;
using System.Diagnostics;
using System.Numerics;

namespace FancadeLoaderLib.Runtime.Functions.Math;

public sealed class LineVsPlaneFunction : IFunction
{
    private readonly RuntimeTerminal _lineFrom;
    private readonly RuntimeTerminal _lineTo;
    private readonly RuntimeTerminal _planePoint;
    private readonly RuntimeTerminal _planeNormal;

    public LineVsPlaneFunction(RuntimeTerminal lineFrom, RuntimeTerminal lineTo, RuntimeTerminal planePoint, RuntimeTerminal planeNormal)
    {
        _lineFrom = lineFrom;
        _lineTo = lineTo;
        _planePoint = planePoint;
        _planeNormal = planeNormal;
    }

    public TerminalOutput GetTerminalOutput(byte3 terminalPos, IRuntimeContext context)
    {
        Debug.Assert(terminalPos == TerminalDef.GetOutPosition(0, 2, 4), $"{nameof(terminalPos)} should be valid.");

        Vector3 lineFrom = _lineFrom.GetOutput(context).GetValue(context).Float3.ToNumerics();
        Vector3 lineTo = _lineTo.GetOutput(context).GetValue(context).Float3.ToNumerics();
        Vector3 planePoint = _planePoint.GetOutput(context).GetValue(context).Float3.ToNumerics();
        Vector3 planeNormal = _planeNormal.GetOutput(context).GetValue(context).Float3.ToNumerics();

        float t = Vector3.Dot(planePoint - lineFrom, planeNormal) / Vector3.Dot(lineTo - lineFrom, planeNormal);
        return new TerminalOutput(new RuntimeValue((lineFrom + (t * (lineTo - lineFrom))).ToFloat3()));
    }
}
