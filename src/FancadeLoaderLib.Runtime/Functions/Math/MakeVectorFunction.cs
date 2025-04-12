using FancadeLoaderLib.Editing;
using MathUtils.Vectors;
using System.Diagnostics;

namespace FancadeLoaderLib.Runtime.Functions.Math;

public sealed class MakeVectorFunction : IFunction
{
    private readonly RuntimeTerminal _x;
    private readonly RuntimeTerminal _y;
    private readonly RuntimeTerminal _z;

    public MakeVectorFunction(RuntimeTerminal x, RuntimeTerminal y, RuntimeTerminal z)
    {
        _x = x;
        _y = y;
        _z = z;
    }

    public TerminalOutput GetTerminalOutput(byte3 terminalPos, IRuntimeContext context)
    {
        Debug.Assert(terminalPos == TerminalDef.GetOutPosition(0, 2, 3), $"{nameof(terminalPos)} should be valid.");

        return new TerminalOutput(new RuntimeValue(new float3(_x.GetOutput(context).GetValue(context).Float, _y.GetOutput(context).GetValue(context).Float, _z.GetOutput(context).GetValue(context).Float)));
    }
}
