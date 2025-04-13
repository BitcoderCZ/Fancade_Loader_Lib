using FancadeLoaderLib.Editing;
using MathUtils.Vectors;
using System.Diagnostics;

namespace FancadeLoaderLib.Runtime.Functions.Control;

public sealed class LoopFunction : IActiveFunction
{
    public static readonly byte3 DoPos = TerminalDef.GetOutPosition(0, 2, 2);
    private static readonly byte3 CounterPos = TerminalDef.GetOutPosition(1, 2, 2);

    private readonly RuntimeTerminal _start;
    private readonly RuntimeTerminal _stop;

    private int _step;
    private int _value;

    public LoopFunction(RuntimeTerminal start, RuntimeTerminal stop)
    {
        _start = start;
        _stop = stop;
    }

    public int Execute(byte3 terminalPos, IRuntimeContext context, Span<byte3> executeNext)
    {
        Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be the before terminal.");

        int start = (int)_start.GetOutput(context).GetValue(context).Float;
        int stop = (int)MathF.Ceiling(_stop.GetOutput(context).GetValue(context).Float);

        _step = stop.CompareTo(start);
        _value = start - _step;

        // do executed by the interpreter
        executeNext[0] = TerminalDef.AfterPosition;

        return 1;
    }

    public TerminalOutput GetTerminalOutput(byte3 terminalPos, IRuntimeContext context)
    {
        Debug.Assert(terminalPos == CounterPos, $"{nameof(terminalPos)} should be valid.");

        return new TerminalOutput(new RuntimeValue((float)_value));
    }

    public bool Step(IRuntimeContext context)
    {
        if (_step == 0)
        {
            return false;
        }

        int stop = (int)MathF.Ceiling(_stop.GetOutput(context).GetValue(context).Float);

        int nextVal = _value + _step;
        if (_step > 0 ? nextVal < stop : nextVal > stop)
        {
            _value = nextVal;
            return true;
        }

        return false;
    }
}
