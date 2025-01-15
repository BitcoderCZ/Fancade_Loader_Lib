// <copyright file="SettingType.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MathUtils.Vectors;

namespace FancadeLoaderLib;

public enum SettingType : byte
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
