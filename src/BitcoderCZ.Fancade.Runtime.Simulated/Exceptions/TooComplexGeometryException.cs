using BitcoderCZ.Fancade.Runtime.Exceptions;

namespace BitcoderCZ.Fancade.Runtime.Simulated.Exceptions;

/// <summary>
/// Thrown when a block's geometry is too complex.
/// </summary>
public sealed class TooComplexGeometryException : FancadeException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TooComplexGeometryException"/> class.
    /// </summary>
    public TooComplexGeometryException()
        : base("Too complex geometry!")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TooComplexGeometryException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public TooComplexGeometryException(string message)
        : base("Too complex geometry!", message)
    {
    }
}
