// <copyright file="FancadeConstants.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace BitcoderCZ.Fancade.Editing.Scripting;

/// <summary>
/// Some fancade constants.
/// </summary>
public static class FancadeConstants
{
    /// <summary>
    /// The maximum difference between 2 numbers to be considered equal.
    /// </summary>
    /// <remarks>
    /// e.g:
    /// MathF.Abs(a - b) &lt; <see cref="EqualsNumbersMaxDifference"/>.
    /// </remarks>
    public const float EqualsNumbersMaxDifference = 0.001f;

    /// <summary>
    /// The maximum difference between 2 vectors, squared, to be considered equal.
    /// </summary>
    /// <remarks>
    /// e.g:
    /// (a - b).LengthSquared() &lt; <see cref="EqualsVectorsMaxDifferenceSquared"/>.
    /// </remarks>
    public const float EqualsVectorsMaxDifferenceSquared = 1.0000001e-06f;

    /// <summary>
    /// The maximum variable name length.
    /// </summary>
    public static readonly int MaxVariableNameLength = 15; // enforced by editor script

    /// <summary>
    /// The maximum comment block text name length.
    /// </summary>
    public static readonly int MaxCommentLength = 15; // enforced by editor script

    /// <summary>
    /// The maximum number of input terminals an output terminal can be connected to.
    /// </summary>
    public static readonly int MaxWireSplits = 8; // enforced by editor script

    /// <summary>
    /// The maximum touch sensor finger index.
    /// </summary>
    public static readonly int TouchSensorMaxFingerIndex = 2;
}
