using BitcoderCZ.Fancade.Runtime.Exceptions;

namespace BitcoderCZ.Fancade.Runtime.Simulated.Exceptions;

public sealed class TooComplexGeometryException : FancadeException
{
    public TooComplexGeometryException()
        : base("Too complex geometry!")
    {
    }

    public TooComplexGeometryException(string message)
        : base("Too complex geometry!", message)
    {
    }
}
