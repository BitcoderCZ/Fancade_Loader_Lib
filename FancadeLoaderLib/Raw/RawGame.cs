using FancadeLoaderLib.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;

namespace FancadeLoaderLib.Raw
{
    public class RawGame
    {
        public static readonly ushort CurrentFileVersion = 31;
        public static readonly ushort CurrentNumbStockPrefabs = 597;

        public string Name;
        public string Author;
        public string Description;

        public ushort IdOffset;

        public readonly List<RawPrefab> Prefabs;

        public RawGame(string name)
        {
            if (name is null) throw new ArgumentNullException(nameof(name));

            Name = name;
            Author = "Unknown Author";
            Description = string.Empty;
            Prefabs = new List<RawPrefab>();
        }

        public RawGame(string name, string author, string description, ushort idOffset, List<RawPrefab> prefabs)
        {
            if (name is null) throw new ArgumentNullException(nameof(name));
            if (author is null) throw new ArgumentNullException(nameof(author));
            if (description is null) throw new ArgumentNullException(nameof(description));
            if (prefabs is null) throw new ArgumentNullException(nameof(prefabs));

            Name = name;
            Author = author;
            Description = description;
            IdOffset = idOffset;
            Prefabs = prefabs;
        }

        public void SaveCompressed(Stream stream)
        {
            using (MemoryStream writerStream = new MemoryStream())
            using (FcBinaryWriter writer = new FcBinaryWriter(writerStream))
            {
                Save(writer);

                Zlib.Compress(writerStream, stream);
            }
        }
        public void Save(FcBinaryWriter writer)
        {
            writer.WriteUInt16(CurrentFileVersion);
            writer.WriteString(Name);
            writer.WriteString(Author);
            writer.WriteString(Description);
            writer.WriteUInt16(CurrentNumbStockPrefabs);

            writer.WriteUInt16((ushort)Prefabs.Count);

            for (int i = 0; i < Prefabs.Count; i++)
                Prefabs[i].Save(writer);
        }

        public static (ushort FileVersion, string Name, string Author, string Description) LoadInfoCompressed(Stream stream)
        {
            // decompress
            FcBinaryReader reader = new FcBinaryReader(new MemoryStream());
            Zlib.Decompress(stream, reader.Stream);
            reader.Reset();

            ushort fileVersion = reader.ReadUInt16();

            string name = reader.ReadString();
            string author = reader.ReadString();
            string description = reader.ReadString();

            return (fileVersion, name, author, description);
        }

        public static RawGame LoadCompressed(Stream stream)
        {
            // decompress
            FcBinaryReader reader = new FcBinaryReader(new MemoryStream());
            Zlib.Decompress(stream, reader.Stream);
            reader.Reset();

            return Load(reader);
        }
        public static RawGame Load(FcBinaryReader reader)
        {
            ushort fileVersion = reader.ReadUInt16();

            if (fileVersion > CurrentFileVersion || fileVersion < 26)
                throw new UnsupportedVersionException(fileVersion);
            else if (fileVersion == 26)
                throw new NotImplementedException("Loading file verison 26 has not yet been implemented.");

            string name = reader.ReadString();
            string author = reader.ReadString();
            string description = reader.ReadString();

            ushort idOffset = reader.ReadUInt16();

            ushort numbPrefabs = reader.ReadUInt16();

            List<RawPrefab> prefabs = new List<RawPrefab>(numbPrefabs);
            for (int i = 0; i < numbPrefabs; i++)
                prefabs.Add(RawPrefab.Load(reader));

            return new RawGame(name, author, description, idOffset, prefabs);
        }

        public override string ToString()
            => $"{{Name: {Name}, Author: {Author}, Description: {Description}}}";
    }
}
