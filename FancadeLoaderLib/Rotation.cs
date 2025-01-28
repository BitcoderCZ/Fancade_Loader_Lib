// <copyright file="Rotation.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MathUtils.Vectors;
using System;

namespace FancadeLoaderLib;

/// <summary>
/// Wrapper over <see cref="float3"/> to represent rotation.
/// </summary>
public readonly struct Rotation : IEquatable<Rotation>
{
	/// <summary>
	/// The value of this rotation.
	/// </summary>
	public readonly float3 Value;

	/// <summary>
	/// Initializes a new instance of the <see cref="Rotation"/> struct.
	/// </summary>
	/// <param name="value">Value of this rotation.</param>
	public Rotation(float3 value)
	{
		Value = value;
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2225:Operator overloads have named alternates", Justification = "Use the value field.")]
	public static explicit operator float3(Rotation a)
		=> a.Value;

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2225:Operator overloads have named alternates", Justification = "Use the constructor.")]
	public static explicit operator Rotation(float3 a)
		=> new Rotation(a);

	public static bool operator ==(Rotation left, Rotation right)
		=> left.Value == right.Value;

	public static bool operator !=(Rotation left, Rotation right)
		=> left.Value != right.Value;

	public readonly bool Equals(Rotation other)
		=> this == other;

	public readonly override bool Equals(object? obj)
		=> obj is Rotation other && this == other;

	public readonly override int GetHashCode()
		=> Value.GetHashCode();
}
