using zlib;

namespace FancadeLoaderLib
{
    public static class Zlib
    {
        public static void Decompress(Stream from, Stream to)
        {
            using ZInputStream stream = new ZInputStream(from);

            using MemoryStream ms = new MemoryStream();
            byte[] buffer = new byte[1024];
            int read;

            while ((read = stream.read(buffer, 0, buffer.Length)) > 0)
                ms.Write(buffer, 0, read);

            ms.Position = 0;
            ms.CopyTo(to);
        }
        public static byte[] Decompress(Stream from)
        {
            using ZInputStream stream = new ZInputStream(from);

            using MemoryStream ms = new MemoryStream();
            byte[] buffer = new byte[1024];
            int read;

            while ((read = stream.read(buffer, 0, buffer.Length)) > 0)
                ms.Write(buffer, 0, read);

            return ms.ToArray();
        }

        public static void Compress(Stream from, Stream to)
        {
            using ZOutputStream stream = new ZOutputStream(to, 9);

            byte[] bytes = new byte[from.Length];
            long pos = from.Position;
            from.Position = 0;
            from.Read(bytes, 0, bytes.Length);
            from.Position = pos;

            stream.Write(bytes, 0, bytes.Length);
        }
        public static byte[] Compress(Stream from)
        {
            from.Position = 0;
            using MemoryStream ms = new MemoryStream();
            using ZOutputStream stream = new ZOutputStream(ms, 9);

            byte[] bytes = new byte[from.Length];
            from.Read(bytes, 0, bytes.Length);

            stream.Write(bytes, 0, bytes.Length);

            return ms.ToArray();
        }
    }
}
