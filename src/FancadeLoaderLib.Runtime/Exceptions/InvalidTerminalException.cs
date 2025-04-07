namespace FancadeLoaderLib.Runtime.Exceptions;

public sealed class InvalidTerminalException : Exception
{
    public InvalidTerminalException(string terminalName)
        : base($"The function does not contain a terminal with the name '{terminalName}'.")
    {
    }
}
