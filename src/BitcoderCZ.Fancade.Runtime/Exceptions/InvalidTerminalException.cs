using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade.Runtime.Exceptions;

/// <summary>
/// A <see cref="FancadeException"/> thrown when a connection points to an invalid terminal.
/// </summary>
public sealed class InvalidTerminalException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidTerminalException"/> class.
    /// </summary>
    /// <param name="terminalName">Name of the non-existent terminal.</param>
    public InvalidTerminalException(string terminalName)
        : base($"The block does not contain a terminal with the name '{terminalName}'.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidTerminalException"/> class.
    /// </summary>
    /// <param name="terminalPosition">Position of the non-existent terminal.</param>
    public InvalidTerminalException(int3 terminalPosition)
        : base($"The block does not contain a terminal at the position: {terminalPosition}.")
    {
    }
}
