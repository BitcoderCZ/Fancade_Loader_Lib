using MathUtils.Vectors;

namespace BitcoderCZ.Fancade.Runtime;

public interface IFunction
{
    TerminalOutput GetTerminalOutput(byte3 terminalPos, IRuntimeContext context);
}
