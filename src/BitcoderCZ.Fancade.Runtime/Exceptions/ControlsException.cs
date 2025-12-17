// <copyright file="ControlsException.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace BitcoderCZ.Fancade.Runtime.Exceptions;

/// <summary>
/// A base class for fancade exceptions caused by controls.
/// </summary>
public abstract class ControlsException : FancadeException
{
    /// <inheritdoc/>
    protected ControlsException(string message)
        : base(message)
    {
    }

    /// <inheritdoc/>
    protected ControlsException(string fancadeMessage, string message)
        : base(fancadeMessage, message)
    {
    }
}
