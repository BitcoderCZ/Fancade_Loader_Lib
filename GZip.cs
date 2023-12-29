using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Fancade.LevelEditor
{
    public static class GZip
    {
        public static Action<Stream, Stream> DecompressMain = decompress;
        private static void decompress(Stream from, Stream to)
        {
            using (GZipStream gz = new GZipStream(from, CompressionMode.Decompress, false)) {
                gz.CopyTo(to);
            }
        }
        public static byte[] Decompress(Stream from)
        {
            using (MemoryStream ms = new MemoryStream()) {
                DecompressMain(from, ms);
                return ms.ToArray();
            }
        }

        public static Action<Stream, Stream> CompressMain = compress;
        private static void compress(Stream from, Stream to)
        {
            using GZipStream destination = new GZipStream(to, CompressionLevel.Optimal, leaveOpen: true);
            from.Position = 0;
            from.CopyTo(destination);
            destination.Flush();
            from.Dispose();
        }
        public static byte[] Compress(Stream from)
        {
            using (MemoryStream ms = new MemoryStream()) {
                CompressMain(from, ms);
                return ms.ToArray();
            }
        }
    }
}
