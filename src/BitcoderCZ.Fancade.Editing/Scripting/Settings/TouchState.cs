// <copyright file="TouchState.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace BitcoderCZ.Fancade.Editing.Scripting.Settings;

/// <summary>
/// Represents a touch state, used by touch sensor.
/// </summary>
public enum TouchState
{
    /// <summary>
    /// The touch is active.
    /// </summary>
    Touching = 0,

    /// <summary>
    /// The touch began this frame.
    /// </summary>
    Begins = 1,

    /// <summary>
    /// The touch ended this frame.
    /// </summary>
    Ends = 2,
}
