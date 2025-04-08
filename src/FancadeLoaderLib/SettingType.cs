// <copyright file="SettingType.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MathUtils.Vectors;
using System.ComponentModel;

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

/// <summary>
/// Utils for <see cref="SettingType"/>.
/// </summary>
#pragma warning disable SA1649 // File name should match first type name - it fucking does???
public static class SettingTypeUtils
#pragma warning restore SA1649
{
    /// <summary>
    /// Gets the corresponding <see cref="SettingType"/> for a terminal's <see cref="SignalType"/>.
    /// </summary>
    /// <param name="type">The terminal's <see cref="SignalType"/>.</param>
    /// <param name="isInput">
    /// If <see langword="true"/>, the terminal is input,
    /// if <see langword="false"/>, the terminal is output.
    /// </param>
    /// <returns>The corresponding <see cref="SettingType"/>.</returns>
    /// <exception cref="InvalidEnumArgumentException">Thrown when <paramref name="type"/> is not a <see cref="SignalType"/>.</exception>
    public static SettingType FromTerminalSignalType(SignalType type, bool isInput)
        => type.ToNotPointer() switch
        {
            SignalType.Void => SettingType.VoidTerminal,
            SignalType.Float => isInput ? SettingType.FloatTerminalIn : SettingType.FloatTerminalOut,
            SignalType.Vec3 => isInput ? SettingType.Vec3TerminalIn : SettingType.Vec3TerminalOut,
            SignalType.Rot => isInput ? SettingType.RotTerminalIn : SettingType.RotTerminalOut,
            SignalType.Bool => isInput ? SettingType.BoolTerminalIn : SettingType.BoolTerminalOut,
            SignalType.Obj => isInput ? SettingType.ObjTerminalIn : SettingType.ObjTerminalOut,
            SignalType.Con => isInput ? SettingType.ConTerminalIn : SettingType.ConTerminalOut,
            _ => throw new InvalidEnumArgumentException(nameof(type), (int)type, typeof(SignalType)),
        };

    /// <summary>
    /// Gets the corresponding <see cref="SignalType"/> for a <see cref="SettingType"/>.
    /// </summary>
    /// <param name="type">The <see cref="SettingType"/>.</param>
    /// <returns>The <see cref="SignalType"/> and a <see langword="bool"/> indicating whether <paramref name="type"/> represents an input or output terminal, <see langword="false"/> for <see cref="SettingType.VoidTerminal"/>.</returns>
    /// <exception cref="InvalidEnumArgumentException">Thrown when <paramref name="type"/> is not a <see cref="SettingType"/> or it is not a <see cref="SettingType"/> that specifies a terminal name.</exception>
    public static (SignalType SignalType, bool IsInput) ToTerminalSignalType(SettingType type)
        => type switch
        {
            SettingType.VoidTerminal => (SignalType.Void, false),
            SettingType.FloatTerminalIn => (SignalType.Float, true),
            SettingType.FloatTerminalOut => (SignalType.Float, false),
            SettingType.Vec3TerminalIn => (SignalType.Vec3, true),
            SettingType.Vec3TerminalOut => (SignalType.Vec3, false),
            SettingType.BoolTerminalIn => (SignalType.Bool, true),
            SettingType.BoolTerminalOut => (SignalType.Bool, false),
            SettingType.RotTerminalIn => (SignalType.Rot, true),
            SettingType.RotTerminalOut => (SignalType.Rot, false),
            SettingType.ObjTerminalIn => (SignalType.Obj, true),
            SettingType.ObjTerminalOut => (SignalType.Obj, false),
            SettingType.ConTerminalIn => (SignalType.Con, true),
            SettingType.ConTerminalOut => (SignalType.Con, false),
            _ => throw new InvalidEnumArgumentException(nameof(type), (int)type, typeof(SettingType)),
        };
}