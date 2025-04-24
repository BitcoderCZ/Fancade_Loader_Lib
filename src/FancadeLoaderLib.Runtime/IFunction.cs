using MathUtils.Vectors;

namespace FancadeLoaderLib.Runtime;

public interface IFunction
{
    TerminalOutput GetTerminalOutput(byte3 terminalPos, IRuntimeContext context);
}
