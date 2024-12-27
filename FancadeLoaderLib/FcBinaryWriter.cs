// <copyright file="FcBinaryWriter.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MathUtils.Vectors;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace FancadeLoaderLib;

public class FcBinaryWriter : IDisposable
{
	private readonly Stream _stream;

	public FcBinaryWriter(byte[] bytes)
	{
		_stream = new MemoryStream(bytes);
		Debug.Assert(_stream.CanWrite, $"{nameof(MemoryStream)} should always be writeable.");
	}

	public FcBinaryWriter(Stream stream)
	{
		if (!stream.CanRead)
		{
			throw new ArgumentException($"{nameof(stream)} isn't writeable.");
		}

		_stream = stream;
	}

	public FcBinaryWriter(string path)
	{
		_stream = new FileStream(path, FileMode.Create, FileAccess.Write);

		if (!_stream.CanRead)
		{
			throw new IOException("Can't write to stream.");
		}
	}

	public long Position { get => _stream.Position; set => _stream.Position = value; }

	public long Length => _stream.Length;

	public void Reset() 
		=> _stream.Position = 0;

	public void WriteBytes(byte[] bytes) 
		=> WriteBytes(bytes, 0, bytes.Length);

	public void WriteBytes(byte[] bytes, int offset, int count) 
		=> _stream.Write(bytes, offset, count);

	public void WriteInt8(sbyte value) 
		=> WriteBytes([(byte)value]);

	public void WriteUInt8(byte value) 
		=> WriteBytes([value]);

	public void WriteInt16(short value) 
		=> WriteBytes(BitConverter.GetBytes(value));

	public void WriteUInt16(ushort value) 
		=> WriteBytes(BitConverter.GetBytes(value));

	public void WriteInt32(int value) 
		=> WriteBytes(BitConverter.GetBytes(value));

	public void WriteUInt32(uint value) 
		=> WriteBytes(BitConverter.GetBytes(value));

	public void WriteInt64(long value)
		=> WriteBytes(BitConverter.GetBytes(value));

	public void WriteUInt64(ulong value) 
		=> WriteBytes(BitConverter.GetBytes(value));

	public void WriteFloat(float value) 
		=> WriteBytes(BitConverter.GetBytes(value));

	public void WriteString(string value)
	{
		byte[] bytes = Encoding.ASCII.GetBytes(value);
		if (bytes.Length > byte.MaxValue)
		{
			throw new ArgumentException($"String too long ({bytes.Length}) max {byte.MaxValue}, string: \"{value}\"", nameof(value));
		}

		WriteUInt8((byte)bytes.Length);
		WriteBytes(bytes);
	}

	public void WriteVec3B(byte3 value)
	{
		WriteUInt8(value.X);
		WriteUInt8(value.Y);
		WriteUInt8(value.Z);
	}

	public void WriteVec3S(short3 value)
	{
		WriteInt16(value.X);
		WriteInt16(value.Y);
		WriteInt16(value.Z);
	}

	public void WriteVec3US(ushort3 value)
	{
		WriteUInt16(value.X);
		WriteUInt16(value.Y);
		WriteUInt16(value.Z);
	}

	public void WriteVec3I(int3 value)
	{
		WriteInt32(value.X);
		WriteInt32(value.Y);
		WriteInt32(value.Z);
	}

	public void WriteVec3F(float3 value)
	{
		WriteFloat(value.X);
		WriteFloat(value.Y);
		WriteFloat(value.Z);
	}

	public void Flush()
		=> _stream.Flush();

	public void Dispose()
	{
		_stream.Close();
		_stream.Dispose();
	}
}
