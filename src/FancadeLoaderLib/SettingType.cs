// <copyright file="SettingType.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MathUtils.Vectors;

namespace FancadeLoaderLib;

/// <summary>
/// Represents the type of a setting.
/// </summary>
#pragma warning disable CA1028 // Enum Storage should be Int32
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1008:Enums should have zero value", Justification = "Not a valid value.")]
public enum SettingType : byte
#pragma warning restore CA1028 // Enum Storage should be Int32
{
    /// <summary>
    /// The type of this setting's value is <see langword="byte"/>.
    /// </summary>
    Byte = 1,

    /// <summary>
    /// The type of this setting's value is <see langword="ushort"/>.
    /// </summary>
    /// 
    Ushort = 2,

    /// <summary>
    /// The type of this setting's value is <see langword="int"/>.
    /// </summary>
    Int = 3,

    /// <summary>
    /// The type of this setting's value is <see langword="float"/>.
    /// </summary>
    Float = 4,

    /// <summary>
    /// The type of this setting's value is <see cref="float3"/> (vector or rotation).
    /// </summary>
    Vec3 = 5,

    /// <summary>
    /// The type of this setting's value is <see langword="string"/>.
    /// </summary>
    String = 6,
}
