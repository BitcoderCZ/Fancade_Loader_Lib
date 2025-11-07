using System.Runtime.CompilerServices;

namespace BitcoderCZ.Fancade.Runtime.Exceptions;

/// <summary>
/// A <see cref="FancadeException"/> thrown when the combination of controls is invalid.
/// </summary>
public sealed class UnsupportedControlConfigurationException : FancadeException
{
    private UnsupportedControlConfigurationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnsupportedControlConfigurationException"/> class, with the text "Two joysticks + buttons are not supported".
    /// </summary>
    /// <param name="sourceBlock">Location of the block that caused the exception.</param>
    /// <returns>The new instance.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UnsupportedControlConfigurationException CreateTwoJoysticksPlusButtons(EnvironmentPosition? sourceBlock)
        => new UnsupportedControlConfigurationException("Two joysticks + buttons are not supported")
        {
            SourceBlock = sourceBlock,
        };
}
