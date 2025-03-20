// <copyright file="Connection.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Utils;
using MathUtils.Vectors;
using System;

namespace FancadeLoaderLib;

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

    public static bool operator ==(Connection left, Connection right)
        => left.From == right.From && left.To == right.To && left.FromVoxel == right.FromVoxel && left.ToVoxel == right.ToVoxel;

    public static bool operator !=(Connection left, Connection right)
        => !(left == right);

    /// <summary>
    /// Loads a <see cref="Connection"/> from a <see cref="FcBinaryReader"/>.
    /// </summary>
    /// <param name="reader">The reader to read the <see cref="Connection"/> from.</param>
    /// <returns>A <see cref="Connection"/> read from <paramref name="reader"/>.</returns>
    public static Connection Load(FcBinaryReader reader)
    {
        if (reader is null)
        {
            ThrowHelper.ThrowArgumentNullException(nameof(reader));
        }

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
        if (writer is null)
        {
            ThrowHelper.ThrowArgumentNullException(nameof(writer));
        }

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
