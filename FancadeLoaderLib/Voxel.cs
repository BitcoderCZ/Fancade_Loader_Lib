// <copyright file="Voxel.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace FancadeLoaderLib;

/// <summary>
/// Represents a single voxel of a prefab.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct Voxel
{
	/// <summary>
	/// Colors of the sides of the voxel in the following order:
	/// <para>+X, -X, +Y, -Y, +Z, -Z.</para>
	/// </summary>
	public Array6<byte> Colors;

	/// <summary>
	/// <see langword="true"/> if the side does NOT have glue/"lego" on it - connects to other voxels; otherwise, <see langword="false"/>./prefabs. 
	/// </summary>
	/// <remarks>
	/// In the same order as <see cref="Colors"/>.
	/// </remarks>
	public Array6<bool> Attribs;

	/// <summary>
	/// Gets a value indicating whether this voxel is empty.
	/// </summary>
	/// <value><see langword="true"/> if this voxel is empty; otherwise, <see langword="false"/>.</value>
	public readonly bool IsEmpty => Colors[0] == 0;

	/// <summary>
	/// Returns the string representation of the current instance.
	/// </summary>
	/// <returns>The string representation of the current instance.</returns>
	public readonly override string ToString()
	{
		StringBuilder builder = new StringBuilder(64);

		builder.Append('[');

		for (int i = 0; i < 6; i++)
		{
			if (i != 0)
			{
				builder.Append(", ");
			}

			builder.Append(Colors[i]);
		}

		builder.Append("; Attribs: ");

		for (int i = 0; i < 6; i++)
		{
			if (i != 0)
			{
				builder.Append(", ");
			}

			builder.Append(Attribs[i]);
		}

		builder.Append(']');

		return builder.ToString();
	}

	[InlineArray(6)]
	public struct Array6<T>
		where T : unmanaged
	{
#pragma warning disable IDE0044 // Add readonly modifier
		private T _element0;
#pragma warning restore IDE0044
	}
}
