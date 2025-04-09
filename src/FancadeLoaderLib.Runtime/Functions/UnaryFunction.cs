using MathUtils.Vectors;

namespace FancadeLoaderLib.Runtime.Functions;

public abstract class UnaryFunction : IFunction
{
    protected readonly RuntimeTerminal Input;

    protected UnaryFunction(RuntimeTerminal input)
    {
        Input = input;
    }

    public abstract TerminalOutput GetTerminalOutput(byte3 terminalPos, IRuntimeContext context);
}
