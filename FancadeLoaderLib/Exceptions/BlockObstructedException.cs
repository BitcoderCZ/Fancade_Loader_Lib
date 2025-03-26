using System;
using System.Collections.Generic;
using System.Text;

namespace FancadeLoaderLib.Exceptions;

/// <summary>
/// The exception that is thrown when a block cannot be placed  because its position is obstructed.
/// </summary>
public sealed class BlockObstructedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BlockObstructedException"/> class.
    /// </summary>
    public BlockObstructedException()
        : base("Cannot place block because its position is obstructed.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BlockObstructedException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public BlockObstructedException(string message)
        : base(message)
    {
    }
}
