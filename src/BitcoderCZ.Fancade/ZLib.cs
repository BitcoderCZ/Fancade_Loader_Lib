// <copyright file="ZLib.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System.Buffers;
using System.IO;
using System.IO.Compression;
using BitcoderCZ.Fancade;

#if !NET6_0_OR_GREATER
using ComponentAce.Compression.Libs.zlib;
#endif

using static BitcoderCZ.Fancade.Utils.ThrowHelper;

namespace BitcoderCZ.Fancade;

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
        byte[] buffer = ArrayPool<byte>.Shared.Rent(1024 * 8);
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
    /// <param name="compressionLevel">
    /// Determines how much will the output be compressed.
    /// <para></para>
    /// <c>-1</c> for default compression; otherwise, <c>0</c> to <c>9</c>.
    /// </param>
    public static void Compress(Stream from, Stream to, int compressionLevel = -1)
    {
        ThrowIfNull(from, nameof(from));
        ThrowIfNull(to, nameof(to));

        if (compressionLevel < -1 || compressionLevel > 9)
        {
            ThrowArgumentOutOfRangeException(nameof(compressionLevel), $"{nameof(compressionLevel)} must be between -1 and 9.");
        }
#if NET6_0_OR_GREATER
        CompressionLevel level = compressionLevel switch
        {
            0 => CompressionLevel.NoCompression,
            1 or 2 or 3 or 4 => CompressionLevel.Fastest,
            5 or 6 or 7 or 8 or 9 => CompressionLevel.SmallestSize,
            _ => CompressionLevel.Optimal,
        };

        using ZLibStream zlib = new ZLibStream(to, level, true);

        from.CopyTo(zlib);
#else
#pragma warning disable CA2000 // ZOutputStream always disposes the underlying stream, which isn't desirelable, so dispose isn't called on it
        ZOutputStream zlib = new ZOutputStream(to, 9);
#pragma warning restore CA2000

        byte[] buffer = ArrayPool<byte>.Shared.Rent(1024 * 8);
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
