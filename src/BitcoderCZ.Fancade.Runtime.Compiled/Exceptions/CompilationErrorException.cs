using Microsoft.CodeAnalysis;

namespace BitcoderCZ.Fancade.Runtime.Compiled.Exceptions;

public sealed class CompilationErrorException : Exception
{
    public CompilationErrorException(IEnumerable<Diagnostic> diagnostics)
    {
        Diagnostics = diagnostics;
    }

    public IEnumerable<Diagnostic> Diagnostics { get; }
}
