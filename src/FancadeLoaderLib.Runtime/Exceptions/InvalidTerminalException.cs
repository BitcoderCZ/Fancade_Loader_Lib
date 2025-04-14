using MathUtils.Vectors;

namespace FancadeLoaderLib.Runtime.Exceptions;

public sealed class InvalidTerminalException : Exception
{
    public InvalidTerminalException(string terminalName)
        : base($"The block does not contain a terminal with the name '{terminalName}'.")
    {
    }

    public InvalidTerminalException(int3 terminalPosition)
        : base($"The block does not contain a terminal at the position: {terminalPosition}.")
    {
    }
}
