// <copyright file="Connection.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MathUtils.Vectors;

namespace FancadeLoaderLib;

/// <summary>
/// Represents a connection (wire) between 2 terminals.
/// </summary>
/// <remarks>
/// if From/To.XYZ == 32769 AND in block -> one side of connection is outside.
/// <para>When connected to a prefab group, <see cref="From"/>/<see cref="To"/> point to the main prefab of the group (<see cref="Prefab.PosInGroup"/> == <see cref="byte3.Zero"/>).</para>
/// </remarks>
public struct Connection
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

	/// <summary>
	/// Loads a <see cref="Connection"/> from a <see cref="FcBinaryReader"/>.
	/// </summary>
	/// <param name="reader">The reader to read the <see cref="Connection"/> from.</param>
	/// <returns>A <see cref="Connection"/> read from <paramref name="reader"/>.</returns>
	public static Connection Load(FcBinaryReader reader)
	{
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
}
