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
	public ushort3 From;
	public ushort3 To;
	public ushort3 FromVoxel; // local position of the connector in SubBlock space
	public ushort3 ToVoxel; // local position of the connector in SubBlock space

	/// <summary>
	/// Initializes a new instance of the <see cref="Connection"/> struct.
	/// </summary>
	/// <param name="from"></param>
	/// <param name="to"></param>
	/// <param name="fromVoxel"></param>
	/// <param name="toVoxel"></param>
	public Connection(ushort3 from, ushort3 to, ushort3 fromVoxel, ushort3 toVoxel)
	{
		From = from;
		To = to;
		FromVoxel = fromVoxel;
		ToVoxel = toVoxel;
	}

	public static Connection Load(FcBinaryReader reader)
	{
		ushort3 from = reader.ReadVec3US();
		ushort3 to = reader.ReadVec3US();
		ushort3 fromVoxel = reader.ReadVec3US();
		ushort3 toVoxel = reader.ReadVec3US();

		return new Connection(from, to, fromVoxel, toVoxel);
	}

	public readonly void Save(FcBinaryWriter writer)
	{
		writer.WriteVec3US(From);
		writer.WriteVec3US(To);
		writer.WriteVec3US(FromVoxel);
		writer.WriteVec3US(ToVoxel);
	}

	public readonly override string ToString()
		=> $"[From: {From}, To: {To}, FromVox: {FromVoxel}, ToVox: {ToVoxel}]";
}
