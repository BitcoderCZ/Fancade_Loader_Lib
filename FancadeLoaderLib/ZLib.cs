// <copyright file="ZLib.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System.Buffers;
using System.IO;
using System.IO.Compression;
#if !NET6_0_OR_GREATER
using ComponentAce.Compression.Libs.zlib;
#endif

namespace FancadeLoaderLib;

/// <summary>
/// Utils for compression and decompressing streams using the zlib format.
/// </summary>
public static class Zlib
{
	/// <summary>
	/// Decompresses a stream into another stream.
	/// </summary>
	/// <param name="from">The compressed stream.</param>
	/// <param name="to">The decompressed stream.</param>
	public static void Decompress(Stream from, Stream to)
	{
#if NET6_0_OR_GREATER
		using ZLibStream zlib = new ZLibStream(from, CompressionMode.Decompress, true);

		zlib.CopyTo(to);
#else
		ZInputStream zlib = new ZInputStream(from);

		using MemoryStream ms = new MemoryStream();
		byte[] buffer = ArrayPool<byte>.Shared.Rent(1024);
		try
		{
			int read;

			while ((read = zlib.read(buffer, 0, buffer.Length)) > 0)
			{
				ms.Write(buffer, 0, read);
			}
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(buffer);
		}

		ms.WriteTo(to);
#endif
	}

	/// <summary>
	/// Compresses a stream into another stream.
	/// </summary>
	/// <param name="from">The source stream.</param>
	/// <param name="to">The compressed stream.</param>
	public static void Compress(Stream from, Stream to)
	{
#if NET6_0_OR_GREATER
		using ZLibStream zlib = new ZLibStream(to, CompressionLevel.SmallestSize, true);

		from.CopyTo(zlib);
#else
		ZOutputStream zlib = new ZOutputStream(to, 9);

		byte[] buffer = ArrayPool<byte>.Shared.Rent(1024);
		try
		{
			int read;

			while ((read = from.Read(buffer, 0, buffer.Length)) > 0)
			{
				zlib.Write(buffer, 0, read);
			}
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(buffer);
			zlib.finish(); // can't call dispose because that would also dispose to
		}
#endif
	}
}
