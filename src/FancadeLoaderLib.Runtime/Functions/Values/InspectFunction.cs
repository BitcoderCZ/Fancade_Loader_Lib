using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Runtime.Exceptions;
using MathUtils.Vectors;
using System.Diagnostics;

namespace FancadeLoaderLib.Runtime.Functions.Values;

public sealed class InspectFunction : IActiveFunction
{
    private readonly ushort3 _blockPosition;
    private readonly RuntimeTerminal _input;

    public InspectFunction(ushort3 blockPosition, RuntimeTerminal input)
    {
        _blockPosition = blockPosition;
        _input = input;
    }

    public int Execute(byte3 terminalPos, IRuntimeContext context, Span<byte3> executeNext)
    {
        Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be the before terminal.");

        var output = _input.GetOutput(context);

        if (output.IsConnected)
        {
            context.InspectValue(_blockPosition, _input.GetOutput(context).GetValue(context));
        }

        executeNext[0] = TerminalDef.AfterPosition;

        return 1;
    }

    public TerminalOutput GetTerminalOutput(byte3 terminalPos, IRuntimeContext context)
        => throw new InvalidTerminalException(terminalPos);
}
