namespace BitcoderCZ.Fancade.Runtime.Exceptions;

/// <summary>
/// The exception that is thrown when the time allotted for a process or operation has expired.
/// </summary>
public sealed class FcTimeoutException : FancadeException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FcTimeoutException"/> class.
    /// </summary>
    public FcTimeoutException()
        : base("Timeout! Infinite loop?")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FcTimeoutException"/> class.
    /// </summary>
    /// <param name="sourceBlock">Location of the block that caused the exception.</param>
    public FcTimeoutException(EnvironmentPosition sourceBlock)
        : base("Timeout! Infinite loop?")
    {
        SourceBlock = sourceBlock;
    }
}
