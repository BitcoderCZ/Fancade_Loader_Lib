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
#if NET8_0_OR_GREATER
public struct Voxel
#else
public unsafe struct Voxel
#endif
{
	/// <summary>
	/// Colors of the sides of the voxel in the following order:
	/// <para>+X, -X, +Y, -Y, +Z, -Z.</para>
	/// </summary>
#if NET8_0_OR_GREATER
	public Array6<byte> Colors;
#else
	public fixed byte Colors[6];
#endif

	/// <summary>
	/// <see langword="true"/> if the side does NOT have glue/"lego" on it - connects to other voxels; otherwise, <see langword="false"/>./prefabs. 
	/// </summary>
	/// <remarks>
	/// In the same order as <see cref="Colors"/>.
	/// </remarks>
#if NET8_0_OR_GREATER
	public Array6<bool> Attribs;
#else
	public fixed bool Attribs[6];
#endif

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

#if NET8_0_OR_GREATER
	/// <summary>
	/// Struct array with 6 items.
	/// </summary>
	/// <typeparam name="T">The item type.</typeparam>
	[InlineArray(6)]
	public struct Array6<T>
		where T : unmanaged
	{
#pragma warning disable IDE0044 // Add readonly modifier
		private T _element0;
#pragma warning restore IDE0044
	}
#endif
}
