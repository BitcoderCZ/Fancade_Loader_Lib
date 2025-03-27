// <copyright file="ZLib.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System.Buffers;
using System.IO;
using System.IO.Compression;

#if !NET6_0_OR_GREATER
using ComponentAce.Compression.Libs.zlib;
#endif
using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib;

/// <summary>
/// Utils for compression and decompressing streams using the zlib format.
/// </summary>
#pragma warning disable CA1724 // consumers aren't expected to use ComponentAce.Compression.Libs.zlib
public static class Zlib
#pragma warning restore CA1724
{
    /// <summary>
    /// Decompresses a stream into another stream.
    /// </summary>
    /// <param name="from">The compressed stream.</param>
    /// <param name="to">The decompressed stream.</param>
    public static void Decompress(Stream from, Stream to)
    {
        ThrowIfNull(from, nameof(from));
        ThrowIfNull(to, nameof(to));

#if NET6_0_OR_GREATER
        using ZLibStream zlib = new ZLibStream(from, CompressionMode.Decompress, true);

        zlib.CopyTo(to);
#else
#pragma warning disable CA2000 // ZInputStream always disposes the underlying stream, which isn't desirelable, so dispose isn't called on it
        ZInputStream zlib = new ZInputStream(from);
#pragma warning restore CA2000

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
        ThrowIfNull(from, nameof(from));
        ThrowIfNull(to, nameof(to));

#if NET6_0_OR_GREATER
        using ZLibStream zlib = new ZLibStream(to, CompressionLevel.SmallestSize, true);

        from.CopyTo(zlib);
#else
#pragma warning disable CA2000 // ZOutputStream always disposes the underlying stream, which isn't desirelable, so dispose isn't called on it
        ZOutputStream zlib = new ZOutputStream(to, 9);
#pragma warning restore CA2000

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
