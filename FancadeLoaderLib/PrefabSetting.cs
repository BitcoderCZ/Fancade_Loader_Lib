// <copyright file="PrefabSetting.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MathUtils.Vectors;
using System;

namespace FancadeLoaderLib;

/// <summary>
/// Represents a "setting" of a prefab.
/// <para>For exaple: if setCamera is perspective, the sound of playSound, ...</para>
/// </summary>
/// <remarks>
/// If value == default, no block value.
/// </remarks>
public struct PrefabSetting
{
	public byte Index;
	public ushort3 Position;

	private SettingType _type;
	private object _value;

	public PrefabSetting(byte index, SettingType type, ushort3 pos, object value)
	{
		if (!IsValueValid(value, type))
		{
			throw new ArgumentException($"Type of value '{value?.GetType()?.FullName ?? "null"}' isn't valid for {nameof(type)} '{type}'", nameof(value));
		}

		Index = index;
		_type = type;
		Position = pos;
		_value = value;
	}

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

	public object Value
	{
		readonly get => _value;
		set
		{
			if (!IsValueValid(value, Type))
			{
				throw new ArgumentException($"Type of value '{value?.GetType()?.FullName ?? "null"}' isn't valid for {nameof(SettingType)} '{Type}'", nameof(value));
			}

			_value = value;
		}
	}

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

	public static PrefabSetting Load(FcBinaryReader reader)
	{
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

		return new PrefabSetting()
		{
			Index = valueIndex,
			Type = type,
			Position = pos,
			Value = value,
		};
	}

	public readonly void Save(FcBinaryWriter writer)
	{
		writer.WriteUInt8(Index);
		writer.WriteUInt8((byte)Type);
		writer.WriteVec3US(Position);

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
				writer.WriteVec3F((float3)Value);
				break;
			case SettingType.String:
			default: // string (string value (6) or terminal name (7+))
				writer.WriteString((string)Value);
				break;
		}
	}

	public void SetValue(SettingType type, object value)
	{
		if (!IsValueValid(value, type))
		{
			throw new ArgumentException($"Type of value '{value?.GetType()?.FullName ?? "null"}' isn't valid for {nameof(type)} '{type}'", nameof(value));
		}

		_type = type;
		_value = value;
	}

	public readonly override string ToString()
		=> $"[Type: {Type}, Value: {Value}, Pos: {Position}]";
}
