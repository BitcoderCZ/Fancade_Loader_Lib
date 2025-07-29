// <copyright file="Connection.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MathUtils.Vectors;
using System;
using static BitcoderCZ.Fancade.Utils.ThrowHelper;

namespace BitcoderCZ.Fancade;

/// <summary>
/// Represents a connection (wire) between 2 terminals.
/// </summary>
/// <remarks>
/// if From/To.XYZ == 32769 AND in block -> one side of connection is outside.
/// <para>When connected to a prefab, <see cref="From"/>/<see cref="To"/> point to the main segment of the prefab (<see cref="PrefabSegment.PosInPrefab"/> == <see cref="byte3.Zero"/>).</para>
/// </remarks>
public struct Connection : IEquatable<Connection>
{
    /// <summary>
    /// The value that a <see cref="Connection"/> has when one of it's sides is on a prefab.
    /// </summary>
    public const ushort IsFromToOutsideValue = 32769;

    /// <summary>
    /// Position of the first block.
    /// </summary>
    public ushort3 From;

    /// <summary>
    /// Position of the second block.
    /// </summary>
    public ushort3 To;

    /// <summary>
    /// Postition of the voxel that this connection connects from, from <see cref="From"/>.
    /// </summary>
    public ushort3 FromVoxel;

    /// <summary>
    /// Postition of the voxel that this connection connects to, from <see cref="To"/>.
    /// </summary>
    public ushort3 ToVoxel;

    /// <summary>
    /// Initializes a new instance of the <see cref="Connection"/> struct.
    /// </summary>
    /// <param name="from">Position of the first block.</param>
    /// <param name="to">Position of the second block.</param>
    /// <param name="fromVoxel">Postition of the voxel that this connection connects from, from <paramref name="from"/>.</param>
    /// <param name="toVoxel">Postition of the voxel that this connection connects to, from <paramref name="to"/>.</param>
    public Connection(ushort3 from, ushort3 to, ushort3 fromVoxel, ushort3 toVoxel)
    {
        From = from;
        To = to;
        FromVoxel = fromVoxel;
        ToVoxel = toVoxel;
    }

    /// <summary>
    /// Gets a value indicating whether the <see cref="Connection"/> is from outside of the prefab.
    /// </summary>
    /// <value><see langword="true"/> if the <see cref="Connection"/> is from outside of the prefab; otherwise, <see langword="false"/>.</value>
    public readonly bool IsFromOutside => From.X == IsFromToOutsideValue;

    /// <summary>
    /// Gets a value indicating whether the <see cref="Connection"/> is to outside of the prefab.
    /// </summary>
    /// <value><see langword="true"/> if the <see cref="Connection"/> is to outside of the prefab; otherwise, <see langword="false"/>.</value>
    public readonly bool IsToOutside => To.X == IsFromToOutsideValue;

    /// <summary>Returns a value that indicates whether the 2 <see cref="Connection"/>s are equal.</summary>
    /// <param name="left">The first <see cref="Connection"/> to compare.</param>
    /// <param name="right">The second <see cref="Connection"/> to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(Connection left, Connection right)
        => left.From == right.From && left.To == right.To && left.FromVoxel == right.FromVoxel && left.ToVoxel == right.ToVoxel;

    /// <summary>Returns a value that indicates whether the 2 <see cref="Connection"/>s are not equal.</summary>
    /// <param name="left">The first <see cref="Connection"/> to compare.</param>
    /// <param name="right">The second <see cref="Connection"/> to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(Connection left, Connection right)
        => !(left == right);

    /// <summary>
    /// Loads a <see cref="Connection"/> from a <see cref="FcBinaryReader"/>.
    /// </summary>
    /// <param name="reader">The reader to read the <see cref="Connection"/> from.</param>
    /// <returns>A <see cref="Connection"/> read from <paramref name="reader"/>.</returns>
    public static Connection Load(FcBinaryReader reader)
    {
        ThrowIfNull(reader, nameof(reader));

        ushort3 from = reader.ReadVec3US();
        ushort3 to = reader.ReadVec3US();
        ushort3 fromVoxel = reader.ReadVec3US();
        ushort3 toVoxel = reader.ReadVec3US();

        return new Connection(from, to, fromVoxel, toVoxel);
    }

    /// <summary>
    /// Writes a <see cref="Connection"/> into a <see cref="FcBinaryWriter"/>.
    /// </summary>
    /// <param name="writer">The <see cref="FcBinaryWriter"/> to write this instance into.</param>
    public readonly void Save(FcBinaryWriter writer)
    {
        ThrowIfNull(writer, nameof(writer));

        writer.WriteUshort3(From);
        writer.WriteUshort3(To);
        writer.WriteUshort3(FromVoxel);
        writer.WriteUshort3(ToVoxel);
    }

    /// <summary>
    /// Returns the string representation of the current instance.
    /// </summary>
    /// <returns>The string representation of the current instance.</returns>
    public readonly override string ToString()
        => $"From: {From}, To: {To}, FromVox: {FromVoxel}, ToVox: {ToVoxel}";

    /// <inheritdoc/>
    public readonly bool Equals(Connection other)
        => this == other;

    /// <inheritdoc/>
    public readonly override bool Equals(object? obj)
        => obj is Connection other && this == other;

    /// <inheritdoc/>
    public readonly override int GetHashCode()
        => HashCode.Combine(From, To, FromVoxel, ToVoxel);
}
