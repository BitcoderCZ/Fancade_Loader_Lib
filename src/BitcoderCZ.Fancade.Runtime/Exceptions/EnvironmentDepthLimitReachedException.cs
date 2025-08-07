namespace BitcoderCZ.Fancade.Runtime.Exceptions;

/// <summary>
/// A <see cref="FancadeException"/> thrown when maximum ast depth (blocks inside blocks) is exceeded.
/// </summary>
public sealed class EnvironmentDepthLimitReachedException : FancadeException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentDepthLimitReachedException"/> class.
    /// </summary>
    public EnvironmentDepthLimitReachedException()
        : base("Too many blocks inside blocks!")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentDepthLimitReachedException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public EnvironmentDepthLimitReachedException(string message)
        : base("Too many blocks inside blocks!", message)
    {
    }
}
