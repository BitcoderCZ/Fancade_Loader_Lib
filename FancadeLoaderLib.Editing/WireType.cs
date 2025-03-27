// <copyright file="WireType.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Numerics;

namespace FancadeLoaderLib.Editing;

/// <summary>
/// Represents the type of a wire/terminal.
/// </summary>
public enum WireType
{
    /// <summary>
    /// Invalid type.
    /// </summary>
    Error = 0,

    /// <summary>
    /// Void type (execution wire).
    /// </summary>
    Void = 1,

    /// <summary>
    /// Float type.
    /// </summary>
    Float = 2,

    /// <summary>
    /// Float type pointer (variable).
    /// </summary>
    FloatPtr = 3,

    /// <summary>
    /// Vector type.
    /// </summary>
    Vec3 = 4,

    /// <summary>
    /// Vector type pointer (variable).
    /// </summary>
    Vec3Ptr = 5,

    /// <summary>
    /// Rotation type.
    /// </summary>
    Rot = 6,

    /// <summary>
    /// Rotation type pointer (variable).
    /// </summary>
    RotPtr = 7,

    /// <summary>
    /// Truth type.
    /// </summary>
    Bool = 8,

    /// <summary>
    /// Truth type pointer (variable).
    /// </summary>
    BoolPtr = 9,

    /// <summary>
    /// Object type.
    /// </summary>
    Obj = 10,

    /// <summary>
    /// Object type pointer (variable).
    /// </summary>
    ObjPtr = 11,

    /// <summary>
    /// Constraint type.
    /// </summary>
    Con = 12,

    /// <summary>
    /// Constraint type pointer (variable).
    /// </summary>
    ConPtr = 13,
}

/// <summary>
/// Utils for <see cref="WireType"/>.
/// </summary>
#pragma warning disable SA1649 // File name should match first type name - it fucking does???
public static class WireTypeUtils
#pragma warning restore SA1649
{
    private static readonly FrozenDictionary<Type, WireType> TypeToWireType = new Dictionary<Type, WireType>()
    {
        [typeof(float)] = WireType.Float,
        [typeof(Vector3)] = WireType.Vec3,
        [typeof(Rotation)] = WireType.Rot,
        [typeof(bool)] = WireType.Bool,
    }.ToFrozenDictionary();

    /// <summary>
    /// Gets the corresponding <see cref="WireType"/> for a <see cref="Type"/>.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to get the <see cref="WireType"/> for.</param>
    /// <returns>The <see cref="WireType"/> corresponding to <paramref name="type"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="type"/> doesn't correspond to any <see cref="WireType"/>.</exception>
    public static WireType FromType(Type type)
        => TypeToWireType.TryGetValue(type, out var wireType)
            ? wireType
            : throw new ArgumentException($"Type '{type?.FullName ?? "null"}' doesn't map to any setting type.", nameof(type));

    /// <summary>
    /// Converts a <see cref="WireType"/> to the pointer version (<see cref="WireType.Float"/> to <see cref="WireType.FloatPtr"/>).
    /// </summary>
    /// <param name="wireType">The <see cref="WireType"/> to convert.</param>
    /// <returns>The converted <see cref="WireType"/>.</returns>
    public static WireType ToPointer(this WireType wireType)
        => wireType == WireType.Error ? wireType : (WireType)((int)wireType | 1);

    /// <summary>
    /// Converts a <see cref="WireType"/> to the non pointer version (<see cref="WireType.FloatPtr"/> to <see cref="WireType.Float"/>).
    /// </summary>
    /// <param name="wireType">The <see cref="WireType"/> to convert.</param>
    /// <returns>The converted <see cref="WireType"/>.</returns>
    public static WireType ToNotPointer(this WireType wireType)
        => wireType == WireType.Void ? wireType : (WireType)((int)wireType & (int.MaxValue ^ 1));

    /// <summary>
    /// Determines if a <see cref="WireType"/> is a pointer.
    /// </summary>
    /// <param name="wireType">The <see cref="WireType"/> to check.</param>
    /// <returns><see langword="true"/> if <paramref name="wireType"/> is a pointer; otherwise, <see langword="false"/>.</returns>
    public static bool IsPointer(this WireType wireType)
        => wireType != WireType.Void && ((int)wireType & 1) == 1;
}