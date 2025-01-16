// <copyright file="FcBinaryReader.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MathUtils.Vectors;
using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace FancadeLoaderLib;

/// <summary>
/// A class for reading primitives in fancade format.
/// </summary>
public class FcBinaryReader : IDisposable
{
	/// <summary>
	/// The underlying stream.
	/// </summary>
	public readonly Stream Stream;

	private readonly bool _leaveOpen;

	/// <summary>
	/// Initializes a new instance of the <see cref="FcBinaryReader"/> class.
	/// </summary>
	/// <param name="bytes">The array to read from.</param>
	public FcBinaryReader(byte[] bytes)
	{
		Stream = new MemoryStream(bytes);
		Debug.Assert(Stream.CanRead, $"{nameof(MemoryStream)} should always be readable.");
		Position = 0;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="FcBinaryReader"/> class.
	/// </summary>
	/// <param name="stream">The stream to read from.</param>
	public FcBinaryReader(Stream stream)
		: this(stream, false)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="FcBinaryReader"/> class.
	/// </summary>
	/// <param name="stream">The stream to read from.</param>
	/// <param name="leaveOpen">If <paramref name="stream"/> should be left open after <see cref="Dispose"/> is called.</param>
	public FcBinaryReader(Stream stream, bool leaveOpen)
	{
		if (!stream.CanRead)
		{
			throw new ArgumentException($"{nameof(stream)} isn't readable.");
		}

		Stream = stream;
		_leaveOpen = leaveOpen;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="FcBinaryReader"/> class.
	/// </summary>
	/// <param name="path">Path to the file to read from, must not be compressed.</param>
	public FcBinaryReader(string path)
	{
		Stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);

		if (!Stream.CanRead)
		{
			throw new IOException("Can't read from stream.");
		}
	}

	/// <summary>
	/// Gets or sets the current position in <see cref="Stream"/>.
	/// </summary>
	/// <value>Current position in <see cref="Stream"/>.</value>
	public long Position { get => Stream.Position; set => Stream.Position = value; }

	/// <summary>
	/// Gets the length of <see cref="Stream"/>.
	/// </summary>
	/// <value>Length of <see cref="Stream"/>.</value>
	public long Length => Stream.Length;

	/// <summary>
	/// Gets the number if bytes left in <see cref="Stream"/>.
	/// </summary>
	/// <value>Number if bytes left in <see cref="Stream"/>.</value>
	public long BytesLeft => Stream.Length - Position;

	/// <summary>
	/// Sets position of <see cref="Stream"/> to 0.
	/// </summary>
	public void Reset()
		=> Stream.Position = 0;

	/// <summary>
	/// Reads a span from the underlying stream.
	/// </summary>
	/// <param name="span">The span to write into.</param>
	/// <exception cref="EndOfStreamException">Thrown when <paramref name="span"/>.Length &gt; <see cref="BytesLeft"/>.</exception>
	public void ReadSpan(Span<byte> span)
	{
		if (BytesLeft < span.Length)
		{
			throw new EndOfStreamException("Reached end of stream.");
		}

		int read = Stream.Read(span);
		Debug.Assert(read == span.Length, "The number of bytes read should always be equal to the length of the span.");
	}

	/// <summary>
	/// Reads an array from the underlying stream.
	/// </summary>
	/// <param name="count">The number of bytes to read.</param>
	/// <returns>A byte array read from the underlying stream.</returns>
	/// <exception cref="EndOfStreamException">Thrown when count &gt; <see cref="BytesLeft"/>.</exception>
	public byte[] ReadBytes(int count)
	{
		if (BytesLeft < count)
		{
			throw new EndOfStreamException("Reached end of stream.");
		}

		byte[] bytes = new byte[count];
		Stream.Read(bytes, 0, count);
		return bytes;
	}

	/// <summary>
	/// Reads an int8 from the underlying stream.
	/// </summary>
	/// <returns>A sbyte read from the underlying stream.</returns>
	/// <exception cref="EndOfStreamException">Thrown when <see cref="BytesLeft"/> &lt; 1.</exception>
	public sbyte ReadInt8()
	{
		Span<byte> span = stackalloc byte[1];
		ReadSpan(span);
		return (sbyte)span[0];
	}

	/// <summary>
	/// Reads a uint8 from the underlying stream.
	/// </summary>
	/// <returns>A byte read from the underlying stream.</returns>
	/// <exception cref="EndOfStreamException">Thrown when <see cref="BytesLeft"/> &lt; 1.</exception>
	public byte ReadUInt8()
	{
		Span<byte> span = stackalloc byte[1];
		ReadSpan(span);
		return span[0];
	}

	/// <summary>
	/// Reads an int16 from the underlying stream.
	/// </summary>
	/// <returns>A short read from the underlying stream.</returns>
	/// <exception cref="EndOfStreamException">Thrown when <see cref="BytesLeft"/> &lt; 2.</exception>
	public short ReadInt16()
	{
		Span<byte> span = stackalloc byte[2];
		ReadSpan(span);
		return BinaryPrimitives.ReadInt16LittleEndian(span);
	}

	/// <summary>
	/// Reads a uint16 from the underlying stream.
	/// </summary>
	/// <returns>A ushort read from the underlying stream.</returns>
	/// <exception cref="EndOfStreamException">Thrown when <see cref="BytesLeft"/> &lt; 2.</exception>
	public ushort ReadUInt16()
	{
		Span<byte> span = stackalloc byte[2];
		ReadSpan(span);
		return BinaryPrimitives.ReadUInt16LittleEndian(span);
	}

	/// <summary>
	/// Reads an int32 from the underlying stream.
	/// </summary>
	/// <returns>An int read from the underlying stream.</returns>
	/// <exception cref="EndOfStreamException">Thrown when <see cref="BytesLeft"/> &lt; 4.</exception>
	public int ReadInt32()
	{
		Span<byte> span = stackalloc byte[4];
		ReadSpan(span);
		return BinaryPrimitives.ReadInt32LittleEndian(span);
	}

	/// <summary>
	/// Reads a uint32 from the underlying stream.
	/// </summary>
	/// <returns>A uint read from the underlying stream.</returns>
	/// <exception cref="EndOfStreamException">Thrown when <see cref="BytesLeft"/> &lt; 4.</exception>
	public uint ReadUInt32()
	{
		Span<byte> span = stackalloc byte[4];
		ReadSpan(span);
		return BinaryPrimitives.ReadUInt32LittleEndian(span);
	}

	/// <summary>
	/// Reads an int64 from the underlying stream.
	/// </summary>
	/// <returns>A long read from the underlying stream.</returns>
	/// <exception cref="EndOfStreamException">Thrown when <see cref="BytesLeft"/> &lt; 8.</exception>
	public long ReadInt64()
	{
		Span<byte> span = stackalloc byte[8];
		ReadSpan(span);
		return BinaryPrimitives.ReadInt64LittleEndian(span);
	}

	/// <summary>
	/// Reads a uint64 from the underlying stream.
	/// </summary>
	/// <returns>A ulong read from the underlying stream.</returns>
	/// <exception cref="EndOfStreamException">Thrown when <see cref="BytesLeft"/> &lt; 8.</exception>
	public ulong ReadUInt64()
	{
		Span<byte> span = stackalloc byte[8];
		ReadSpan(span);
		return BinaryPrimitives.ReadUInt64LittleEndian(span);
	}

	/// <summary>
	/// Reads a single from the underlying stream.
	/// </summary>
	/// <returns>A float read from the underlying stream.</returns>
	/// <exception cref="EndOfStreamException">Thrown when <see cref="BytesLeft"/> &lt; 4.</exception>
	public float ReadFloat()
	{
		Span<byte> span = stackalloc byte[4];
		ReadSpan(span);
		return BinaryPrimitives.ReadSingleLittleEndian(span);
	}

	/// <summary>
	/// Reads a string from the underlying stream.
	/// </summary>
	/// <returns>A string read from the underlying stream.</returns>
	/// <exception cref="EndOfStreamException">Thrown when <see cref="BytesLeft"/> is less than 1 + length of the string.</exception>
	public string ReadString()
	{
		byte length = ReadUInt8();
		Span<byte> buffer = stackalloc byte[255];
		buffer = buffer[..length];

		ReadSpan(buffer);

		return Encoding.ASCII.GetString(buffer);
	}

	/// <summary>
	/// Reads a byte3 from the underlying stream.
	/// </summary>
	/// <returns>A byte3 read from the underlying stream.</returns>
	/// <exception cref="EndOfStreamException">Thrown when <see cref="BytesLeft"/> &lt; 3.</exception>
	public byte3 ReadVec3B()
		=> new byte3(ReadUInt8(), ReadUInt8(), ReadUInt8());

	/// <summary>
	/// Reads a short3 from the underlying stream.
	/// </summary>
	/// <returns>A short3 read from the underlying stream.</returns>
	/// <exception cref="EndOfStreamException">Thrown when <see cref="BytesLeft"/> &lt; 6.</exception>
	public short3 ReadVec3S()
		=> new short3(ReadInt16(), ReadInt16(), ReadInt16());

	/// <summary>
	/// Reads a ushort3 from the underlying stream.
	/// </summary>
	/// <returns>A ushort3 read from the underlying stream.</returns>
	/// <exception cref="EndOfStreamException">Thrown when <see cref="BytesLeft"/> &lt; 6.</exception>
	public ushort3 ReadVec3US()
		=> new ushort3(ReadUInt16(), ReadUInt16(), ReadUInt16());

	/// <summary>
	/// Reads a int3 from the underlying stream.
	/// </summary>
	/// <returns>A int3 read from the underlying stream.</returns>
	/// <exception cref="EndOfStreamException">Thrown when <see cref="BytesLeft"/> &lt; 12.</exception>
	public int3 ReadVec3I()
		=> new int3(ReadInt32(), ReadInt32(), ReadInt32());

	/// <summary>
	/// Reads a float3 from the underlying stream.
	/// </summary>
	/// <returns>A float3 read from the underlying stream.</returns>
	/// <exception cref="EndOfStreamException">Thrown when <see cref="BytesLeft"/> &lt; 12.</exception>
	public float3 ReadVec3F()
		=> new float3(ReadFloat(), ReadFloat(), ReadFloat());

	/// <summary>
	/// Releases all the resources used by the <see cref="FcBinaryReader"/>.
	/// </summary>
	public void Dispose()
	{
		if (!_leaveOpen)
		{
			Stream.Close();
			Stream.Dispose();
		}
	}
}
