// <copyright file="TooManyObjectsException.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace BitcoderCZ.Fancade.Runtime.Exceptions;

/// <summary>
/// A <see cref="FancadeException"/> thrown a prefab receives an invalid input.
/// </summary>
public sealed class TooManyObjectsException : FancadeException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TooManyObjectsException"/> class.
    /// </summary>
    /// <param name="sourceBlock">Location of the block that caused the exception.</param>
    public TooManyObjectsException(EnvironmentPosition sourceBlock)
        : base("Too many objects!")
    {
        SourceBlock = sourceBlock;
    }
}