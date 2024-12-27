// <copyright file="FcBinaryReader.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MathUtils.Vectors;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace FancadeLoaderLib;

public class FcBinaryReader : IDisposable
{
	public readonly Stream Stream;

	public FcBinaryReader(byte[] bytes)
	{
		Stream = new MemoryStream(bytes);
		Debug.Assert(Stream.CanRead, $"{nameof(MemoryStream)} should always be readable.");
		Position = 0;
	}

	public FcBinaryReader(Stream stream)
	{
		if (!stream.CanRead)
		{
			throw new ArgumentException($"{nameof(stream)} isn't readable.");
		}

		Stream = stream;

		Position = 0;
	}

	public FcBinaryReader(string path)
	{
		Stream = new FileStream(path, FileMode.Open, FileAccess.Read);

		if (!Stream.CanRead)
		{
			throw new IOException("Can't read from stream.");
		}

		Position = 0;
	}

	public long Position { get => Stream.Position; set => Stream.Position = value; }

	public long Length => Stream.Length;

	public long BytesLeft => Stream.Length - Position;

	/// <summary>
	/// Sets position of <see cref="Stream"/> to 0.
	/// </summary>
	public void Reset()
		=> Stream.Position = 0;

	public byte[] ReadBytes(int count)
	{
		if (BytesLeft < count)
		{
			throw new EndOfStreamException("Reached end of stream");
		}

		byte[] bytes = new byte[count];
		Stream.Read(bytes, 0, count);
		return bytes;
	}

	public sbyte ReadInt8()
		=> (sbyte)ReadBytes(1)[0];

	public byte ReadUInt8()
		=> ReadBytes(1)[0];

	public short ReadInt16()
		=> BitConverter.ToInt16(ReadBytes(2), 0);

	public ushort ReadUInt16()
		=> BitConverter.ToUInt16(ReadBytes(2), 0);

	public int ReadInt32()
		=> BitConverter.ToInt32(ReadBytes(4), 0);

	public uint ReadUInt32()
		=> BitConverter.ToUInt32(ReadBytes(4), 0);

	public long ReadInt64()
		=> BitConverter.ToInt64(ReadBytes(8), 0);

	public ulong ReadUInt64()
		=> BitConverter.ToUInt64(ReadBytes(8), 0);

	public float ReadFloat()
		=> BitConverter.ToSingle(ReadBytes(4), 0);

	public string ReadString()
	{
		byte length = ReadUInt8();
		return Encoding.ASCII.GetString(ReadBytes(length));
	}

	public byte3 ReadVec3B()
		=> new byte3(ReadUInt8(), ReadUInt8(), ReadUInt8());

	public short3 ReadVec3S()
		=> new short3(ReadInt16(), ReadInt16(), ReadInt16());

	public ushort3 ReadVec3US()
		=> new ushort3(ReadUInt16(), ReadUInt16(), ReadUInt16());

	public int3 ReadVec3I()
		=> new int3(ReadInt32(), ReadInt32(), ReadInt32());

	public float3 ReadVec3F()
		=> new float3(ReadFloat(), ReadFloat(), ReadFloat());

	public void Dispose()
	{
		Stream.Close();
		Stream.Dispose();
	}
}
