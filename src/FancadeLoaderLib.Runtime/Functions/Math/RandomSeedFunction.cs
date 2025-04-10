using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Runtime.Exceptions;
using MathUtils.Vectors;
using System.Diagnostics;

namespace FancadeLoaderLib.Runtime.Functions.Math;

public sealed class RandomSeedFunction : IActiveFunction
{
    private readonly RuntimeTerminal _input;

    public RandomSeedFunction(RuntimeTerminal input)
    {
        _input = input;
    }

    public int Execute(byte3 terminalPos, IRuntimeContext context, Span<byte3> executeNext)
    {
        Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be the before terminal.");

        var output = _input.GetOutput(context);

        if (output.IsConnected)
        {
            context.SetRandomSeed(_input.GetOutput(context).GetValue(context).Float);
        }

        executeNext[0] = TerminalDef.AfterPosition;

        return 1;
    }

    public TerminalOutput GetTerminalOutput(byte3 terminalPos, IRuntimeContext context)
        => throw new InvalidTerminalException(terminalPos);
}
