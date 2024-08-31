using MathUtils.Vectors;
using System;
using System.IO;
using System.Text;

namespace FancadeLoaderLib
{
    public class FcBinaryWriter : IDisposable
    {
        private Stream stream;

        public long Position { get => stream.Position; set => stream.Position = value; }

        public long Length => stream.Length;

        public FcBinaryWriter(byte[] _bytes)
        {
            stream = new MemoryStream(_bytes);
            if (!stream.CanWrite)
                throw new Exception("Can't write to stream");
            Position = 0;
        }

        public FcBinaryWriter(Stream _stream)
        {
            stream = _stream;
            if (!stream.CanWrite)
                throw new Exception("Can't write to stream");
            Position = 0;
        }

        public FcBinaryWriter(string _path, bool clear)
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

        public void WriteVec3B(Vector3B value)
        {
            WriteUInt8(value.X);
            WriteUInt8(value.Y);
            WriteUInt8(value.Z);
        }

        public void WriteVec3S(Vector3S value)
        {
            WriteInt16(value.X);
            WriteInt16(value.Y);
            WriteInt16(value.Z);
        }
        public void WriteVec3US(Vector3US value)
        {
            WriteUInt16(value.X);
            WriteUInt16(value.Y);
            WriteUInt16(value.Z);
        }

        public void WriteVec3I(Vector3I value)
        {
            WriteInt32(value.X);
            WriteInt32(value.Y);
            WriteInt32(value.Z);
        }

        public void WriteVec3F(Vector3F value)
        {
            WriteFloat(value.X);
            WriteFloat(value.Y);
            WriteFloat(value.Z);
        }

        public void Flush() => stream.Flush();

        public void Dispose()
        {
            stream.Close();
            stream.Dispose();
        }
    }
}
