namespace BitcoderCZ.Fancade.Runtime.Exceptions;

public sealed class EnvironmentDepthLimitReachedException : FancadeException
{
    public EnvironmentDepthLimitReachedException()
        : base("Too many blocks inside blocks!")
    {
    }

    public EnvironmentDepthLimitReachedException(string message)
        : base("Too many blocks inside blocks!", message)
    {
    }
}
