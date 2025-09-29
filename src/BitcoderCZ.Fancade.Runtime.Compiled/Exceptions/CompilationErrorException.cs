using Microsoft.CodeAnalysis;

namespace BitcoderCZ.Fancade.Runtime.Compiled.Exceptions;

/// <summary>
/// An <see cref="Exception"/> thrown when the transpiled c# code has errors.
/// </summary>
public sealed class CompilationErrorException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompilationErrorException"/> class.
    /// </summary>
    /// <param name="diagnostics">The diagnostics produces by the transpiled code.</param>
    public CompilationErrorException(IEnumerable<Diagnostic> diagnostics)
    {
        Diagnostics = diagnostics;
    }

    /// <summary>
    /// Gets the diagnostics produces by the transpiled code.
    /// </summary>
    /// <value>The diagnostics produces by the transpiled code.</value>
    public IEnumerable<Diagnostic> Diagnostics { get; }
}
