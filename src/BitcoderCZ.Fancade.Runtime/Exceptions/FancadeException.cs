// <copyright file="FancadeException.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace BitcoderCZ.Fancade.Runtime.Exceptions;

/// <summary>
/// A base class for all fancade exceptions.
/// </summary>
public abstract class FancadeException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FancadeException"/> class.
    /// </summary>
    /// <param name="message">The message that should be shown to the user.</param>
    protected FancadeException(string message)
        : base(message)
    {
        FancadeMessage = message;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FancadeException"/> class.
    /// </summary>
    /// <param name="fancadeMessage">The message that should be shown to the user.</param>
    /// <param name="message">The message that describes the error.</param>
    protected FancadeException(string fancadeMessage, string message)
        : base(message)
    {
        FancadeMessage = fancadeMessage;
    }

    /// <summary>
    /// Gets the message that should be shown to the user, same as in Fancade.
    /// </summary>
    /// <value>The message that should be shown to the user.</value>
    public string FancadeMessage { get; }

    /// <summary>
    /// Gets the location of the block that caused the exception.
    /// </summary>
    /// <value>Location of the block that caused the exception; or <see langword="null"/>, if the exception was not caused by a specific block.</value>
    public EnvironmentPosition? SourceBlock { get; init; }
}
