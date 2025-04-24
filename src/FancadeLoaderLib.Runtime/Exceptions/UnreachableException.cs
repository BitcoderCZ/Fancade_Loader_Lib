#pragma warning disable IDE0130
namespace System.Diagnostics;
#pragma warning restore IDE0130

#if NETSTANDARD2_1
internal sealed class UnreachableException : Exception
{
    public UnreachableException()
        : base()
    {
    }

    public UnreachableException(string? message)
        : base(message)
    {
    }
}
#endif