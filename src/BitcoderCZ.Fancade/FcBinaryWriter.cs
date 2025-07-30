// <copyright file="FcBinaryWriter.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Maths.Vectors;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Text;
using static BitcoderCZ.Fancade.Utils.ThrowHelper;

namespace BitcoderCZ.Fancade;

/// <summary>
/// A class for writing primitives in fancade format.
/// </summary>
public sealed class FcBinaryWriter : IDisposable
{
    /// <summary>
    /// The underlying stream.
    /// </summary>
    public readonly Stream Stream;

    private readonly bool _leaveOpen;

    /// <summary>
    /// Initializes a new instance of the <see cref="FcBinaryWriter"/> class.
    /// </summary>
    /// <param name="bytes">The array to write to.</param>
    public FcBinaryWriter(byte[] bytes)
    {
        Stream = new MemoryStream(bytes);
        Debug.Assert(Stream.CanWrite, $"{nameof(MemoryStream)} should always be writeable.");
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FcBinaryWriter"/> class.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    public FcBinaryWriter(Stream stream)
        : this(stream, false)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FcBinaryWriter"/> class.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="leaveOpen">If <paramref name="stream"/> should be left open after <see cref="Dispose"/> is called.</param>
    public FcBinaryWriter(Stream stream, bool leaveOpen)
    {
        ThrowIfNull(stream, nameof(stream));

        if (!stream.CanRead)
        {
            ThrowArgumentException($"{nameof(stream)} isn't writeable.");
        }

        Stream = stream;
        _leaveOpen = leaveOpen;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FcBinaryWriter"/> class.
    /// </summary>
    /// <param name="path">Path of the file to write to.</param>
    public FcBinaryWriter(string path)
    {
        Stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);

        if (!Stream.CanRead)
        {
            ThrowArgumentException("Can't write to stream.");
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
    /// Resets <see cref="Position"/> to 0.
    /// </summary>
    public void Reset()
        => Stream.Position = 0;

    /// <summary>
    /// Writes a byte array to the underlying stream.
    /// </summary>
    /// <param name="value">The value to write.</param>
    public void WriteBytes(byte[] value)
    {
        ThrowIfNull(value, nameof(value));

        WriteBytes(value, 0, value.Length);
    }

    /// <summary>
    /// Writes a byte array to the underlying stream.
    /// </summary>
    /// <param name="value">The value to write.</param>
    /// <param name="offset">The start index in <paramref name="value"/> to start reading from.</param>
    /// <param name="count">The number of items to read from <paramref name="value"/>.</param>
    public void WriteBytes(byte[] value, int offset, int count)
        => Stream.Write(value, offset, count);

    /// <summary>
    /// Writes a span to the underlying stream.
    /// </summary>
    /// <param name="value">The value to write.</param>
    public void WriteSpan(ReadOnlySpan<byte> value)
        => Stream.Write(value);

    /// <summary>
    /// Writes an int8 to the underlying stream.
    /// </summary>
    /// <param name="value">The value to write.</param>
    public void WriteInt8(sbyte value)
        => WriteSpan([(byte)value]);

    /// <summary>
    /// Writes a uint8 to the underlying stream.
    /// </summary>
    /// <param name="value">The value to write.</param>
    public void WriteUInt8(byte value)
        => WriteSpan([value]);

    /// <summary>
    /// Writes an int16 to the underlying stream.
    /// </summary>
    /// <param name="value">The value to write.</param>
    public void WriteInt16(short value)
    {
        Span<byte> buffer = stackalloc byte[2];
        BinaryPrimitives.WriteInt16LittleEndian(buffer, value);
        WriteSpan(buffer);
    }

    /// <summary>
    /// Writes a uint16 to the underlying stream.
    /// </summary>
    /// <param name="value">The value to write.</param>
    public void WriteUInt16(ushort value)
    {
        Span<byte> buffer = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16LittleEndian(buffer, value);
        WriteSpan(buffer);
    }

    /// <summary>
    /// Writes an int32 to the underlying stream.
    /// </summary>
    /// <param name="value">The value to write.</param>
    public void WriteInt32(int value)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(buffer, value);
        WriteSpan(buffer);
    }

    /// <summary>
    /// Writes a uint32 to the underlying stream.
    /// </summary>
    /// <param name="value">The value to write.</param>
    public void WriteUInt32(uint value)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, value);
        WriteSpan(buffer);
    }

    /// <summary>
    /// Writes an int64 to the underlying stream.
    /// </summary>
    /// <param name="value">The value to write.</param>
    public void WriteInt64(long value)
    {
        Span<byte> buffer = stackalloc byte[8];
        BinaryPrimitives.WriteInt64LittleEndian(buffer, value);
        WriteSpan(buffer);
    }

    /// <summary>
    /// Writes a uint64 to the underlying stream.
    /// </summary>
    /// <param name="value">The value to write.</param>
    public void WriteUInt64(ulong value)
    {
        Span<byte> buffer = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64LittleEndian(buffer, value);
        WriteSpan(buffer);
    }

    /// <summary>
    /// Writes a single to the underlying stream.
    /// </summary>
    /// <param name="value">The value to write.</param>
    public void WriteFloat(float value)
    {
        Span<byte> buffer = stackalloc byte[4];
#if NET5_0_OR_GREATER
        BinaryPrimitives.WriteSingleLittleEndian(buffer, value);
#else
        BinaryPrimitives.WriteInt32LittleEndian(buffer, BitConverter.SingleToInt32Bits(value));
#endif
        WriteSpan(buffer);
    }

    /// <summary>
    /// Writes a string to the underlying stream.
    /// </summary>
    /// <param name="value">The value to write, must be shorter than 256.</param>
    public void WriteString(string value)
    {
        Span<byte> buffer = stackalloc byte[byte.MaxValue + 1];
        int written = Encoding.ASCII.GetBytes(value, buffer);
        if (written > byte.MaxValue)
        {
            ThrowArgumentException($"{nameof(value)}, when encoded as ASCII is too long, maximum length is 255.", nameof(value));
        }

        WriteUInt8((byte)written);
        WriteSpan(buffer[..written]);
    }

    /// <summary>
    /// Writes a byte3 to the underlying stream.
    /// </summary>
    /// <param name="value">The value to write.</param>
    public void WriteByte3(byte3 value)
    {
        WriteUInt8(value.X);
        WriteUInt8(value.Y);
        WriteUInt8(value.Z);
    }

    /// <summary>
    /// Writes a short3 to the underlying stream.
    /// </summary>
    /// <param name="value">The value to write.</param>
    public void WriteShort3(short3 value)
    {
        WriteInt16(value.X);
        WriteInt16(value.Y);
        WriteInt16(value.Z);
    }

    /// <summary>
    /// Writes a ushort3 to the underlying stream.
    /// </summary>
    /// <param name="value">The value to write.</param>
    public void WriteUshort3(ushort3 value)
    {
        WriteUInt16(value.X);
        WriteUInt16(value.Y);
        WriteUInt16(value.Z);
    }

    /// <summary>
    /// Writes a int3 to the underlying stream.
    /// </summary>
    /// <param name="value">The value to write.</param>
    public void WriteInt3(int3 value)
    {
        WriteInt32(value.X);
        WriteInt32(value.Y);
        WriteInt32(value.Z);
    }

    /// <summary>
    /// Writes a float3 to the underlying stream.
    /// </summary>
    /// <param name="value">The value to write.</param>
    public void WriteFloat3(float3 value)
    {
        WriteFloat(value.X);
        WriteFloat(value.Y);
        WriteFloat(value.Z);
    }

    /// <summary>
    /// Flushes the underlying stream.
    /// </summary>
    public void Flush()
        => Stream.Flush();

    /// <summary>
    /// Releases all the resources used by the <see cref="FcBinaryWriter"/>.
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
