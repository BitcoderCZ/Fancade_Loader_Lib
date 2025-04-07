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

    /// <summary>
    /// The type of this setting's value is <see langword="string"/>.
    /// <para></para>
    /// Specifies the name of a terminal of type <see cref="SignalType.Void"/>.
    /// </summary>
    VoidTerminal = 7,

    /// <summary>
    /// The type of this setting's value is <see langword="string"/>.
    /// <para></para>
    /// Specifies the name of an <b>in</b> terminal of type <see cref="SignalType.Float"/>.
    /// </summary>
    FloatTerminalIn = 8,

    /// <summary>
    /// The type of this setting's value is <see langword="string"/>.
    /// <para></para>
    /// Specifies the name of an <b>out</b> terminal of type <see cref="SignalType.Float"/>.
    /// </summary>
    FloatTerminalOut = 9,

    /// <summary>
    /// The type of this setting's value is <see langword="string"/>.
    /// <para></para>
    /// Specifies the name of an <b>in</b> terminal of type <see cref="SignalType.Vec3"/>.
    /// </summary>
    Vec3TerminalIn = 10,

    /// <summary>
    /// The type of this setting's value is <see langword="string"/>.
    /// <para></para>
    /// Specifies the name of an <b>out</b> terminal of type <see cref="SignalType.Vec3"/>.
    /// </summary>
    Vec3TerminalOut = 11,

    /// <summary>
    /// The type of this setting's value is <see langword="string"/>.
    /// <para></para>
    /// Specifies the name of an <b>in</b> terminal of type <see cref="SignalType.Rot"/>.
    /// </summary>
    RotTerminalIn = 12,

    /// <summary>
    /// The type of this setting's value is <see langword="string"/>.
    /// <para></para>
    /// Specifies the name of an <b>out</b> terminal of type <see cref="SignalType.Rot"/>.
    /// </summary>
    RotTerminalOut = 13,

    /// <summary>
    /// The type of this setting's value is <see langword="string"/>.
    /// <para></para>
    /// Specifies the name of an <b>in</b> terminal of type <see cref="SignalType.Bool"/>.
    /// </summary>
    BoolTerminalIn = 14,

    /// <summary>
    /// The type of this setting's value is <see langword="string"/>.
    /// <para></para>
    /// Specifies the name of an <b>out</b> terminal of type <see cref="SignalType.Bool"/>.
    /// </summary>
    BoolTerminalOut = 15,

    /// <summary>
    /// The type of this setting's value is <see langword="string"/>.
    /// <para></para>
    /// Specifies the name of an <b>in</b> terminal of type <see cref="SignalType.Obj"/>.
    /// </summary>
    ObjTerminalIn = 16,

    /// <summary>
    /// The type of this setting's value is <see langword="string"/>.
    /// <para></para>
    /// Specifies the name of an <b>out</b> terminal of type <see cref="SignalType.Obj"/>.
    /// </summary>
    ObjTerminalOut = 17,

    /// <summary>
    /// The type of this setting's value is <see langword="string"/>.
    /// <para></para>
    /// Specifies the name of an <b>in</b> terminal of type <see cref="SignalType.Con"/>.
    /// </summary>
    ConTerminalIn = 18,

    /// <summary>
    /// The type of this setting's value is <see langword="string"/>.
    /// <para></para>
    /// Specifies the name of an <b>out</b> terminal of type <see cref="SignalType.Con"/>.
    /// </summary>
    ConTerminalOut = 19,
}
