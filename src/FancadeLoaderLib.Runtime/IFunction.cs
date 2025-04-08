using MathUtils.Vectors;

namespace FancadeLoaderLib.Runtime;

public interface IFunction
{
    TerminalOutput GetTerminalValue(byte3 terminalPos, IRuntimeContext context);
}
