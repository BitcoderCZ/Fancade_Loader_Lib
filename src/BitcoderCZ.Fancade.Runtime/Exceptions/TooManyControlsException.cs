using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace BitcoderCZ.Fancade.Runtime.Exceptions;

/// <summary>
/// A <see cref="FancadeException"/> thrown when there are too many controls active at once.
/// </summary>
public sealed class TooManyControlsException : ControlsException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TooManyControlsException"/> class.
    /// </summary>
    private TooManyControlsException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TooManyControlsException"/> class, with the text "Too many controls!".
    /// </summary>
    /// <param name="sourceBlock">Location of the block that caused the exception.</param>
    /// <returns>The new instance.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TooManyControlsException CreateTooManyControls(EnvironmentPosition? sourceBlock)
        => new TooManyControlsException("Too many controls!")
        {
            SourceBlock = sourceBlock,
        };

    /// <summary>
    /// Initializes a new instance of the <see cref="TooManyControlsException"/> class, with the text "Too many joysticks!".
    /// </summary>
    /// <param name="sourceBlock">Location of the block that caused the exception.</param>
    /// <returns>The new instance.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TooManyControlsException CreateTooManyJoysticks(EnvironmentPosition? sourceBlock)
        => new TooManyControlsException("Too many joysticks!")
        {
            SourceBlock = sourceBlock,
        };
}
