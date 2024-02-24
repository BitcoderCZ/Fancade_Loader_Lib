using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FancadeLoaderLib
{
    public class SaveWriter : IDisposable
    {
        private Stream stream;

        public long Position { get => stream.Position; set => stream.Position = value; }

        public long Length => stream.Length;

        public SaveWriter(byte[] _bytes)
        {
            stream = new MemoryStream(_bytes);
            if (!stream.CanWrite)
                throw new Exception("Can't write to stream");
            Position = 0;
        }

        public SaveWriter(Stream _stream)
        {
            stream = _stream;
            if (!stream.CanWrite)
                throw new Exception("Can't write to stream");
            Position = 0;
        }

        public SaveWriter(string _path, bool clear)
        {
            if (!File.Exists(_path) || clear)
                File.WriteAllBytes(_path, new byte[] { });

            stream = new FileStream(_path, FileMode.OpenOrCreate, FileAccess.Write);
            if (!stream.CanWrite)
                throw new Exception("Can't write to stream");
            Position = 0;
        }

        public void Reset() => stream.Position = 0;

        public void WriteBytes(byte[] bytes) => WriteBytes(bytes, 0, bytes.Length);
        public void WriteBytes(byte[] bytes, int offset, int count)
        {
            stream.Write(bytes, offset, count);
        }

        public void WriteInt8(sbyte value)
        {
            WriteBytes(new byte[] { (byte)value });
        }

        public void WriteUInt8(byte value)
        {
            WriteBytes(new byte[] { value });
        }

        public void WriteInt16(Int16 value)
        {
            WriteBytes(BitConverter.GetBytes(value));
        }

        public void WriteUInt16(UInt16 value)
        {
            WriteBytes(BitConverter.GetBytes(value));
        }

        public void WriteInt32(Int32 value)
        {
            WriteBytes(BitConverter.GetBytes(value));
        }

        public void WriteUInt32(UInt32 value)
        {
            WriteBytes(BitConverter.GetBytes(value));
        }

        public void WriteInt64(Int64 value)
        {
            WriteBytes(BitConverter.GetBytes(value));
        }

        public void WriteUInt64(UInt64 value)
        {
            WriteBytes(BitConverter.GetBytes(value));
        }

        public void WriteFloat(float value)
        {
            WriteBytes(BitConverter.GetBytes(value));
        }

        public void WriteString(string value)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(value);
            if (bytes.Length > byte.MaxValue)
                throw new Exception($"String too long ({bytes.Length}) max {byte.MaxValue}, string: \"{value}\"");
            WriteUInt8((byte)bytes.Length);
            WriteBytes(bytes);
        }

        public void Flush() => stream.Flush();

        public void Dispose()
        {
            stream.Close();
            stream.Dispose();
        }
    }
}
