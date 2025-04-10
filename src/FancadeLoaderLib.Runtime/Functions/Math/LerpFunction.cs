using FancadeLoaderLib.Editing;
using MathUtils.Vectors;
using System.Diagnostics;
using System.Numerics;

namespace FancadeLoaderLib.Runtime.Functions.Math;

public sealed class LerpFunction : IFunction
{
    private readonly RuntimeTerminal _from;
    private readonly RuntimeTerminal _to;
    private readonly RuntimeTerminal _amount;

    public LerpFunction(RuntimeTerminal from, RuntimeTerminal to, RuntimeTerminal amount)
    {
        _from = from;
        _to = to;
        _amount = amount;
    }

    public TerminalOutput GetTerminalOutput(byte3 terminalPos, IRuntimeContext context)
    {
        Debug.Assert(terminalPos == TerminalDef.GetOutPosition(0, 2, 3), $"{nameof(terminalPos)} should be valid.");

        return new TerminalOutput(new RuntimeValue(Quaternion.Lerp(_from.GetOutput(context).GetValue(context).Quaternion, _to.GetOutput(context).GetValue(context).Quaternion, _amount.GetOutput(context).GetValue(context).Float)));
    }
}
