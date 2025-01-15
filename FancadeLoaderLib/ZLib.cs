// <copyright file="ZLib.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System.IO;
using System.IO.Compression;

namespace FancadeLoaderLib;

public static class Zlib
{
	public static void Decompress(Stream from, Stream to)
	{
		using ZLibStream zlib = new ZLibStream(from, CompressionMode.Decompress, true);

		zlib.CopyTo(to);
	}

	public static void Compress(Stream from, Stream to)
	{
		using ZLibStream zlib = new ZLibStream(to, CompressionLevel.SmallestSize, true);

		from.CopyTo(zlib);
	}
}
