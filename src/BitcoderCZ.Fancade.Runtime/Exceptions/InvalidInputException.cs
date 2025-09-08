namespace BitcoderCZ.Fancade.Runtime.Exceptions;

/// <summary>
/// A <see cref="FancadeException"/> thrown a prefab receives an invalid input.
/// </summary>
public sealed class InvalidInputException : FancadeException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidInputException"/> class.
    /// </summary>
    /// <param name="blockName">Name of the prefab that received the invalid input.</param>
    public InvalidInputException(string blockName)
        : base($"{blockName} got invalid (inf or nan) input!")
    {
    }
}