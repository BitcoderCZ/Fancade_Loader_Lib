﻿// <copyright file="PrefabSetting.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Maths.Vectors;
using System.Numerics;
using static BitcoderCZ.Fancade.Utils.ThrowHelper;

namespace BitcoderCZ.Fancade;

/// <summary>
/// Represents a "setting" of a prefab.
/// <para>For exaple: if setCamera is perspective, the sound of playSound, ...</para>
/// </summary>
/// <remarks>
/// If value == default, no block value.
/// </remarks>
public struct PrefabSetting : IEquatable<PrefabSetting>
{
    private SettingType _type;
    private object _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="PrefabSetting"/> struct.
    /// </summary>
    /// <param name="index">The index of this setting.</param>
    /// <param name="type">Type of this setting.</param>
    /// <param name="pos">The position of the block this setting applies to.</param>
    /// <param name="value">Value of this setting.</param>
    public PrefabSetting(byte index, SettingType type, ushort3 pos, object value)
    {
        if (!IsValueValid(value, type))
        {
            ThrowArgumentException($"Type of value '{value?.GetType()?.FullName ?? "null"}' isn't valid for type '{type}'", nameof(value));
        }

        Index = index;
        _type = type;
        Position = pos;
        _value = value;
    }

    /// <summary>
    /// Gets or sets the index of the <see cref="PrefabSetting"/>.
    /// </summary>
    /// <remarks>
    /// Used when a block has multiple settings.
    /// </remarks>
    /// <value>Index of the <see cref="PrefabSetting"/>.</value>
    public byte Index { get; set; }

    /// <summary>
    /// Gets the position of the block this setting applies to.
    /// </summary>
    /// <remarks>
    /// When applied to a prefab larger than 1 segment, points to the segment whose id is equal to <see cref="Prefab.Id"/>.
    /// <para></para>
    /// When <see cref="Type"/> is a terminal name, specifies it's voxel position.
    /// </remarks>
    /// <value>Position of the block this setting applies to.</value>
    public readonly ushort3 Position { get; init; }

    /// <summary>
    /// Gets or sets the type of this setting.
    /// </summary>
    /// <value>Type of this setting.</value>
    public SettingType Type
    {
        readonly get => _type;
        set
        {
            if (!IsValueValid(Value, value))
            {
                _value = GetDefaultValueForType(value);
            }

            _type = value;
        }
    }

    /// <summary>
    /// Gets or sets the value of this setting.
    /// </summary>
    /// <value>Value of this setting.</value>
    public object Value
    {
        readonly get => _value;
        set
        {
            if (!IsValueValid(value, Type))
            {
                ThrowArgumentException($"Type of value '{value?.GetType()?.FullName ?? "null"}' isn't valid for {nameof(SettingType)} '{Type}'", nameof(value));
            }

            _value = value;
        }
    }

    /// <summary>Returns a value that indicates whether the 2 <see cref="PrefabSetting"/>s are equal.</summary>
    /// <param name="left">The first <see cref="PrefabSetting"/> to compare.</param>
    /// <param name="right">The second <see cref="PrefabSetting"/> to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(PrefabSetting left, PrefabSetting right)
        => left.Index == right.Index && left.Position == right.Position && left.Type == right.Type && Equals(left.Value, right.Value);

    /// <summary>Returns a value that indicates whether the 2 <see cref="PrefabSetting"/>s are not equal.</summary>
    /// <param name="left">The first <see cref="PrefabSetting"/> to compare.</param>
    /// <param name="right">The second <see cref="PrefabSetting"/> to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(PrefabSetting left, PrefabSetting right)
        => left.Index != right.Index || left.Position != right.Position || left.Type != right.Type || !Equals(left.Value, right.Value);

    /// <summary>
    /// Gets if a value is valid for a <see cref="SettingType"/>.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <param name="type">The type to test <paramref name="value"/> for.</param>
    /// <returns><see langword="true"/> if <paramref name="value"/> is valid for <paramref name="type"/>; otherwise, <see langword="false"/>.</returns>
    public static bool IsValueValid(object value, SettingType type)
        => type switch
        {
            SettingType.Byte => value is byte,
            SettingType.Ushort => value is ushort,
            SettingType.Int => value is int,
            SettingType.Float => value is float,
            SettingType.Vec3 => value is float3,
            _ => value is string,
        };

    /// <summary>
    /// Gets the default value for a <see cref="SettingType"/>.
    /// </summary>
    /// <param name="type">The type to get the default value for.</param>
    /// <returns>The default value for <paramref name="type"/>.</returns>
    public static object GetDefaultValueForType(SettingType type)
        => type switch
        {
            SettingType.Byte => default(byte),
            SettingType.Ushort => default(ushort),
            SettingType.Int => default(int),
            SettingType.Float => default(float),
            SettingType.Vec3 => default(float3),
            _ => string.Empty,
        };

    /// <summary>
    /// Gets the <see cref="SettingType"/> for <paramref name="value"/>.
    /// </summary>
    /// <param name="value">The type to get the type of.</param>
    /// <returns>The <see cref="SettingType"/> for <paramref name="value"/> or null if <paramref name="value"/> isn't a valid <see cref="PrefabSetting"/> value.</returns>
    public static SettingType? GetTypeForValue(object value)
        => value switch
        {
            byte => SettingType.Byte,
            ushort => SettingType.Ushort,
            int => SettingType.Int,
            float => SettingType.Float,
            Vector3 or Rotation => SettingType.Vec3,
            string => SettingType.String,
            _ => null,
        };

    /// <summary>
    /// Loads a <see cref="PrefabSetting"/> from a <see cref="FcBinaryReader"/>.
    /// </summary>
    /// <param name="reader">The reader to read the <see cref="PrefabSetting"/> from.</param>
    /// <returns>A <see cref="PrefabSetting"/> read from <paramref name="reader"/>.</returns>
    public static PrefabSetting Load(FcBinaryReader reader)
    {
        ThrowIfNull(reader, nameof(reader));

        byte valueIndex = reader.ReadUInt8();
        SettingType type = (SettingType)reader.ReadUInt8();
        ushort3 pos = reader.ReadVec3US();
        object value = type switch
        {
            SettingType.Byte => reader.ReadUInt8(),
            SettingType.Ushort => reader.ReadUInt16(),
            SettingType.Int => reader.ReadInt32(),
            SettingType.Float => reader.ReadFloat(),
            SettingType.Vec3 => new float3(reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat()), // vec3 or rot (euler angles)
            _ => reader.ReadString(), // string (string value (6) or terminal name (7+))
        };

        return new PrefabSetting(valueIndex, type, pos, value);
    }

    /// <summary>
    /// Writes a <see cref="PrefabSetting"/> into a <see cref="FcBinaryWriter"/>.
    /// </summary>
    /// <param name="writer">The <see cref="FcBinaryWriter"/> to write this instance into.</param>
    public readonly void Save(FcBinaryWriter writer)
    {
        ThrowIfNull(writer, nameof(writer));

        writer.WriteUInt8(Index);
        writer.WriteUInt8((byte)Type);
        writer.WriteUshort3(Position);

        switch (Type)
        {
            case SettingType.Byte: // byte
                writer.WriteUInt8((byte)Value);
                break;
            case SettingType.Ushort: // ushort
                writer.WriteUInt16((ushort)Value);
                break;
            case SettingType.Int: // int
                writer.WriteInt32((int)Value);
                break;
            case SettingType.Float: // float
                writer.WriteFloat((float)Value);
                break;
            case SettingType.Vec3: // vec3 or rot (euler angles)
                writer.WriteFloat3((float3)Value);
                break;
            case SettingType.String:
            default: // string (string value (6) or terminal name (7+))
                writer.WriteString((string)Value);
                break;
        }
    }

    /// <summary>
    /// Sets the value and type of this setting.
    /// </summary>
    /// <param name="type">The new type of this setting.</param>
    /// <param name="value">The new value of this setting.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is not valid for <paramref name="type"/>.</exception>
    public void SetValue(SettingType type, object value)
    {
        if (!IsValueValid(value, type))
        {
            ThrowArgumentException($"Type of value '{value?.GetType()?.FullName ?? "null"}' isn't valid for {nameof(type)} '{type}'", nameof(value));
        }

        _type = type;
        _value = value;
    }

    /// <summary>
    /// Returns the string representation of the current instance.
    /// </summary>
    /// <returns>The string representation of the current instance.</returns>
    public readonly override string ToString()
        => $"Type: {Type}, Value: {Value}, Pos: {Position}";

    /// <inheritdoc/>
    public readonly bool Equals(PrefabSetting other)
        => this == other;

    /// <inheritdoc/>
    public readonly override bool Equals(object? obj)
        => obj is PrefabSetting other && this == other;

    /// <inheritdoc/>
    public readonly override int GetHashCode()
        => HashCode.Combine(Index, Position, Type, Value);
}
