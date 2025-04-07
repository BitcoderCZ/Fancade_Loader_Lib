using FancadeLoaderLib.Runtime.Exceptions;
using MathUtils.Vectors;
using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib.Runtime.Functions;

public sealed class InspectFunction : IActiveFunction
{
    private readonly int3 _blockPosition;
    private readonly RuntimeTerminal _input;

    public InspectFunction(int3 blockPosition, RuntimeTerminal input)
    {
        _blockPosition = blockPosition;
        _input = input;
    }

    public int Execute(IRuntimeContext context, Span<string> executeNext)
    {
        if (executeNext.Length < 0)
        {
            ThrowArgumentException($"{nameof(executeNext)}.Length must be at least 1.", nameof(executeNext));
        }

        context.InspectValue(_blockPosition, _input.GetOutput(context).GetValue(context));

        executeNext[0] = "After";

        return 1;
    }

    public TerminalOutput GetTerminalValue(string name, IRuntimeContext context)
        => throw new InvalidTerminalException(name);
}
