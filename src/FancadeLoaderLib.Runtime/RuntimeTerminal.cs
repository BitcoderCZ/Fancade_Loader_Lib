using MathUtils.Vectors;

namespace FancadeLoaderLib.Runtime;

public readonly struct RuntimeTerminal
{
    public RuntimeTerminal(IFunction? function, byte3 position)
    {
        Function = function;
        Position = position;
    }

    public readonly IFunction? Function { get; }

    public readonly byte3 Position { get; }

    public readonly TerminalOutput GetOutput(IRuntimeContext context)
        => Function is null ? TerminalOutput.Disconnected : Function.GetTerminalValue(Position, context);
}
