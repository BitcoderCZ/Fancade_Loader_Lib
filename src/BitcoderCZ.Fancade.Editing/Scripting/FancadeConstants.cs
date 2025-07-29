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
