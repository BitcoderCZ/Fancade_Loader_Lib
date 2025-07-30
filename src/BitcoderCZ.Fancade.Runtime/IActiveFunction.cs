using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade.Runtime;

public interface IActiveFunction : IFunction
{
    int Execute(byte3 terminalPos, IRuntimeContext context, Span<byte3> executeNext);
}
