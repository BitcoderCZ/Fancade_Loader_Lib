using FancadeLoaderLib.Runtime.Exceptions;

namespace FancadeLoaderLib.Runtime.Bullet.Exceptions;

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
