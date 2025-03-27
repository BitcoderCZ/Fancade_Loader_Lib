// <copyright file="PrefabType.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FancadeLoaderLib;

/// <summary>
/// Represents the type of a prefab.
/// </summary>
public enum PrefabType : byte
{
    /// <summary>
    /// The prefab isn't affected by physics.
    /// </summary>
    Normal = 0,

    /// <summary>
    /// The prefab is affected by physics.
    /// </summary>
    Physics = 1,

    /// <summary>
    /// The prefab isn't affected by physics and isn't visible when in play mode.
    /// </summary>
    Script = 2,

    /// <summary>
    /// The prefab is a level.
    /// </summary>
    Level = 3,
}
