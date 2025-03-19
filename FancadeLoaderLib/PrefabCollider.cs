// <copyright file="PrefabCollider.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FancadeLoaderLib;

/// <summary>
/// Represents the type of a prefab's collider.
/// </summary>
#pragma warning disable CA1028 // Enum Storage should be Int32
public enum PrefabCollider : byte
#pragma warning restore CA1028 // Enum Storage should be Int32
{
    /// <summary>
    /// The prefab doesn't have a collider.
    /// </summary>
    None = 0,

    /// <summary>
    /// Collider of the prefabs matches it's voxels.
    /// </summary>
    Box = 1,

    /// <summary>
    /// Collider of the prefab is a sphere.
    /// </summary>
    Sphere = 2,
}
