// <copyright file="EnvironmentPosition.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade.Runtime;

/// <summary>
/// Represents a position in an environment.
/// </summary>
public readonly struct EnvironmentPosition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentPosition"/> struct.
    /// </summary>
    /// <param name="environment">The environment.</param>
    /// <param name="position">Position inside the environment.</param>
    public EnvironmentPosition(IFcEnvironment environment, int3 position)
    {
        Environment = environment;
        Position = position;
    }

    /// <summary>
    /// Gets the environment.
    /// </summary>
    /// <value>The environment.</value>
    public readonly IFcEnvironment Environment { get; }

    /// <summary>
    /// Gets the position inside the environment.
    /// </summary>
    /// <value>Position inside the environment.</value>
    public readonly int3 Position { get; }
}
