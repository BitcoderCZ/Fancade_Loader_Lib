using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Runtime.Exceptions;
using MathUtils.Vectors;
using System.Diagnostics;

namespace FancadeLoaderLib.Runtime.Functions.Values;

public sealed class InspectFunction : IActiveFunction
{
    private readonly RuntimeTerminal _input;
    private readonly SignalType _type;
    private readonly ushort3 _blockPosition;

    public InspectFunction(RuntimeTerminal input, SignalType type, ushort3 blockPosition)
    {
        _input = input;
        _type = type;
        _blockPosition = blockPosition;
    }

    public int Execute(byte3 terminalPos, IRuntimeContext context, Span<byte3> executeNext)
    {
        Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be the before terminal.");

        var output = _input.GetOutput(context);

        if (output.IsConnected)
        {
            context.InspectValue(_input.GetOutput(context), _type, _blockPosition);
        }

        executeNext[0] = TerminalDef.AfterPosition;

        return 1;
    }

    public TerminalOutput GetTerminalOutput(byte3 terminalPos, IRuntimeContext context)
        => throw new InvalidTerminalException(terminalPos);
}
