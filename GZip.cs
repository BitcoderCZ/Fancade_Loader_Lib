using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using zlib;

namespace FancadeLoaderLib
{
    public static class Zlib
    {
        public static void Decompress(Stream from, Stream to)
        {
            using ZInputStream stream = new ZInputStream(from);

            byte[] res = new byte[stream.TotalOut];
            stream.read(res, 0, res.Length);

            to.Write(res, 0, res.Length);
        }

        public static void Compress(Stream from, Stream to)
        {
            from.Position = 0;
            using ZOutputStream stream = new ZOutputStream(to);

            byte[] bytes = new byte[from.Length];
            from.Read(bytes, 0, bytes.Length);

            stream.Write(bytes, 0, bytes.Length);
        }
    }
}
