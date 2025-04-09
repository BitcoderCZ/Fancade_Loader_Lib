using MathUtils.Vectors;

namespace FancadeLoaderLib.Runtime.Functions;

public abstract class BinaryFunction : IFunction
{
    protected readonly RuntimeTerminal Input1;
    protected readonly RuntimeTerminal Input2;

    protected BinaryFunction(RuntimeTerminal input1, RuntimeTerminal input2)
    {
        Input1 = input1;
        Input2 = input2;
    }

    public abstract TerminalOutput GetTerminalOutput(byte3 terminalPos, IRuntimeContext context);
}
