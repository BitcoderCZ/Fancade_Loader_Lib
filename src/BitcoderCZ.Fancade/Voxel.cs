// <copyright file="Voxel.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace BitcoderCZ.Fancade;

/// <summary>
/// Represents a single voxel of a <see cref="PrefabSegment"/>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
#if NET8_0_OR_GREATER
public struct Voxel : IEquatable<Voxel>
#else
public unsafe struct Voxel : IEquatable<Voxel>
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
    /// <see langword="true"/> if the side does NOT have glue/"lego" on it - connects to other voxels; otherwise, <see langword="false"/>. 
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
    /// Initializes a new instance of the <see cref="Voxel"/> struct.
    /// </summary>
    /// <param name="color">The color to assign to all the sides.</param>
    /// <param name="attrib">The attrib to assign to all the sides.</param>
    public Voxel(byte color, bool attrib)
    {
        Colors[0] = color;
        Colors[1] = color;
        Colors[2] = color;
        Colors[3] = color;
        Colors[4] = color;
        Colors[5] = color;

        Attribs[0] = attrib;
        Attribs[1] = attrib;
        Attribs[2] = attrib;
        Attribs[3] = attrib;
        Attribs[4] = attrib;
        Attribs[5] = attrib;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Voxel"/> struct.
    /// </summary>
    /// <param name="color">The color to assign to all the sides.</param>
    /// <param name="attrib">The attrib to assign to all the sides.</param>
    public Voxel(FcColor color, bool attrib)
    {
        byte colorByte = (byte)color;

        Colors[0] = colorByte;
        Colors[1] = colorByte;
        Colors[2] = colorByte;
        Colors[3] = colorByte;
        Colors[4] = colorByte;
        Colors[5] = colorByte;

        Attribs[0] = attrib;
        Attribs[1] = attrib;
        Attribs[2] = attrib;
        Attribs[3] = attrib;
        Attribs[4] = attrib;
        Attribs[5] = attrib;
    }

    /// <summary>
    /// Gets a value indicating whether this voxel is empty.
    /// </summary>
    /// <value><see langword="true"/> if this voxel is empty; otherwise, <see langword="false"/>.</value>
    public readonly bool IsEmpty => Colors[0] == 0;

    /// <summary>Returns a value that indicates whether the 2 <see cref="Voxel"/>s are equal.</summary>
    /// <param name="left">The first <see cref="Voxel"/> to compare.</param>
    /// <param name="right">The second <see cref="Voxel"/> to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(Voxel left, Voxel right)
        => left.Colors[0] == right.Colors[0] &&
           left.Colors[1] == right.Colors[1] &&
           left.Colors[2] == right.Colors[2] &&
           left.Colors[3] == right.Colors[3] &&
           left.Colors[4] == right.Colors[4] &&
           left.Colors[5] == right.Colors[5] &&
           left.Attribs[0] == right.Attribs[0] &&
           left.Attribs[1] == right.Attribs[1] &&
           left.Attribs[2] == right.Attribs[2] &&
           left.Attribs[3] == right.Attribs[3] &&
           left.Attribs[4] == right.Attribs[4] &&
           left.Attribs[5] == right.Attribs[5];

    /// <summary>Returns a value that indicates whether the 2 <see cref="Voxel"/>s are not equal.</summary>
    /// <param name="left">The first <see cref="Voxel"/> to compare.</param>
    /// <param name="right">The second <see cref="Voxel"/> to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(Voxel left, Voxel right)
        => left.Colors[0] != right.Colors[0] ||
           left.Colors[1] != right.Colors[1] ||
           left.Colors[2] != right.Colors[2] ||
           left.Colors[3] != right.Colors[3] ||
           left.Colors[4] != right.Colors[4] ||
           left.Colors[5] != right.Colors[5] ||
           left.Attribs[0] != right.Attribs[0] ||
           left.Attribs[1] != right.Attribs[1] ||
           left.Attribs[2] != right.Attribs[2] ||
           left.Attribs[3] != right.Attribs[3] ||
           left.Attribs[4] != right.Attribs[4] ||
           left.Attribs[5] != right.Attribs[5];

    /// <inheritdoc/>
    public readonly override int GetHashCode()
    {
        HashCode hash = default;

        for (int i = 0; i < 6; i++)
        {
            hash.Add(Colors[i]);
        }

        for (int i = 0; i < 6; i++)
        {
            hash.Add(Attribs[i]);
        }

        return hash.ToHashCode();
    }

    /// <inheritdoc/>
    public readonly bool Equals(Voxel other)
        => this == other;

    /// <inheritdoc/>
    public readonly override bool Equals([NotNullWhen(true)] object? obj)
        => obj is Voxel other && this == other;

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
    /// Value array with 6 items.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    [InlineArray(6)]
#pragma warning disable CA1034 // Nested types should not be visible
#pragma warning disable CA1815 // Override equals and operator equals on value types
    public struct Array6<T>
#pragma warning restore CA1815
#pragma warning restore CA1034
        where T : unmanaged
    {
#pragma warning disable IDE0044 // Add readonly modifier
        private T _element0;
#pragma warning restore IDE0044
    }
#endif
}
