// <copyright file="ZLib.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System.IO;
using System.IO.Compression;

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
		using ZLibStream zlib = new ZLibStream(from, CompressionMode.Decompress, true);

		zlib.CopyTo(to);
	}

	/// <summary>
	/// Compresses a stream into another stream.
	/// </summary>
	/// <param name="from">The source stream.</param>
	/// <param name="to">The compressed stream.</param>
	public static void Compress(Stream from, Stream to)
	{
		using ZLibStream zlib = new ZLibStream(to, CompressionLevel.SmallestSize, true);

		from.CopyTo(zlib);
	}
}
