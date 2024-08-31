using MathUtils.Vectors;
using System;
using System.IO;
using System.Text;

namespace FancadeLoaderLib
{
    public class FcBinaryReader : IDisposable
    {
        public readonly Stream Stream;

        public long Position { get => Stream.Position; set => Stream.Position = value; }

        public long Length => Stream.Length;

        public long BytesLeft => Stream.Length - Position;

        public FcBinaryReader(byte[] _bytes)
        {
            Stream = new MemoryStream(_bytes);
            if (!Stream.CanRead)
                throw new Exception("Can't read from stream");
            Position = 0;
        }

        public FcBinaryReader(Stream _stream)
        {
            Stream = _stream;
            if (!Stream.CanRead)
                throw new Exception("Can't read from stream");
            Position = 0;
        }

        public FcBinaryReader(string _path)
        {
            if (!File.Exists(_path))
                throw new FileNotFoundException($"File \"{_path}\" doesn't exist", _path);

            Stream = new FileStream(_path, FileMode.Open, FileAccess.Read);
            if (!Stream.CanRead)
                throw new Exception("Can't read from stream");
            Position = 0;
        }

        /// <summary>
        /// Sets position of <see cref="Stream"/> to 0
        /// </summary>
        public void Reset() => Stream.Position = 0;

        public byte[] ReadBytes(int count)
        {
            if (BytesLeft < count)
                throw new EndOfStreamException("Reached end of stream");

            byte[] bytes = new byte[count];
            Stream.Read(bytes, 0, count);
            return bytes;
        }

        public sbyte ReadInt8()
        {
            return (sbyte)ReadBytes(1)[0];
        }

        public byte ReadUInt8()
        {
            return ReadBytes(1)[0];
        }

        public Int16 ReadInt16()
        {
            return BitConverter.ToInt16(ReadBytes(2), 0);
        }

        public UInt16 ReadUInt16()
        {
            return BitConverter.ToUInt16(ReadBytes(2), 0);
        }

        public Int32 ReadInt32()
        {
            return BitConverter.ToInt32(ReadBytes(4), 0);
        }

        public UInt32 ReadUInt32()
        {
            return BitConverter.ToUInt32(ReadBytes(4), 0);
        }

        public Int64 ReadInt64()
        {
            return BitConverter.ToInt64(ReadBytes(8), 0);
        }

        public UInt64 ReadUInt64()
        {
            return BitConverter.ToUInt64(ReadBytes(8), 0);
        }

        public float ReadFloat()
        {
            return BitConverter.ToSingle(ReadBytes(4), 0);
        }

        public string ReadString()
        {
            byte length = ReadUInt8();
            return Encoding.ASCII.GetString(ReadBytes(length));
        }

        public Vector3B ReadVec3B()
            => new Vector3B(ReadUInt8(), ReadUInt8(), ReadUInt8());

        public Vector3S ReadVec3S()
            => new Vector3S(ReadInt16(), ReadInt16(), ReadInt16());
        public Vector3US ReadVec3US()
            => new Vector3US(ReadUInt16(), ReadUInt16(), ReadUInt16());

        public Vector3I ReadVec3I()
            => new Vector3I(ReadInt32(), ReadInt32(), ReadInt32());

        public Vector3F ReadVec3F()
            => new Vector3F(ReadFloat(), ReadFloat(), ReadFloat());

        public void Dispose()
        {
            Stream.Close();
            Stream.Dispose();
        }
    }
}
