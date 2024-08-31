using MathUtils.Vectors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace FancadeLoaderLib
{
    public class RawPrefab
    {
        #region Header
        public bool HasConnections;
        public bool HasSettings;
        public bool HasBlocks;
        public bool HasVoxels;
        /// <summary>
        /// True for custom blocks larger than 1x1x1
        /// </summary>
        public bool IsInGroup;
        public bool HasColliderByte;
        public bool UnEditable;
        public bool UnEditable2;
        public bool NonDefaultBackgroundColor;
        public bool HasData2;
        public bool HasData1;
        public bool NonDefaultName;
        public bool HasTypeByte;
        #endregion

        public byte TypeByte;
        public string Name;
        public byte Data1;
        public uint Data2;
        public byte BackgroundColor;
        public byte ColliderByte;
        public ushort GroupId;
        public Vector3B PosInGroup;

        public byte[]? Voxels;
        public Array3D<ushort>? Blocks;
        public List<PrefabSetting>? Settings;
        public List<Connection>? Connections;

        public RawPrefab()
        {
            Name = "";
        }

        public RawPrefab(bool hasConnections, bool hasSettings, bool hasBlocks, bool hasVoxels, bool isInGroup, bool hasColliderByte, bool unEditable, bool unEditable2, bool nonDefaultBackgroundColor, bool hasData2, bool hasData1, bool nonDefaultName, bool hasTypeByte, byte typeByte, string name, byte data1, uint data2, byte backgroundColor, byte colliderByte, ushort groupId, Vector3B posInGroup, byte[]? voxels, Array3D<ushort>? blocks, List<PrefabSetting>? settings, List<Connection>? connections)
        {
            HasConnections = hasConnections;
            HasSettings = hasSettings;
            HasBlocks = hasBlocks;
            HasVoxels = hasVoxels;
            IsInGroup = isInGroup;
            HasColliderByte = hasColliderByte;
            UnEditable = unEditable;
            UnEditable2 = unEditable2;
            NonDefaultBackgroundColor = nonDefaultBackgroundColor;
            HasData2 = hasData2;
            HasData1 = hasData1;
            NonDefaultName = nonDefaultName;
            HasTypeByte = hasTypeByte;
            TypeByte = typeByte;
            Name = name;
            Data1 = data1;
            Data2 = data2;
            BackgroundColor = backgroundColor;
            ColliderByte = colliderByte;
            GroupId = groupId;
            PosInGroup = posInGroup;
            Voxels = voxels;
            Blocks = blocks;
            Settings = settings;
            Connections = connections;
        }

        public unsafe void Save(FcBinaryWriter writer)
        {
            // actually ushort, but I'd need to cast it everywhere, so I used int
            int header = 0;

            if (HasTypeByte)
                header |= 0b0001000000000000;

            if (NonDefaultName)
                header |= 0b100000000000;

            if (NonDefaultBackgroundColor)
                header |= 0b100000000;

            if (UnEditable2)
                header |= 0b10000000;
            if (UnEditable)
                header |= 0b1000000;

            if (HasColliderByte)
                header |= 0b100000;

            if (IsInGroup)
                header |= 0b10000;

            if (HasVoxels)
                header |= 0b1000;

            if (HasBlocks)
                header |= 0b100;

            if (HasSettings)
                header |= 0b10;

            if (HasConnections)
                header |= 0b1;

            writer.WriteUInt16((ushort)header);

            if (HasTypeByte)
                writer.WriteUInt8(TypeByte);

            if (NonDefaultName)
                writer.WriteString(Name);

            if (NonDefaultBackgroundColor)
                writer.WriteUInt8(BackgroundColor);

            if (HasColliderByte)
                writer.WriteUInt8(ColliderByte);

            if (IsInGroup)
            {
                writer.WriteUInt16(GroupId);
                writer.WriteVec3B(PosInGroup);
            }

            if (HasVoxels)
                writer.WriteBytes(Voxels!);

            if (HasBlocks)
            {
                writer.WriteVec3US(new Vector3US(Blocks!.LengthX, Blocks!.LengthY, Blocks!.LengthZ));

                ushort[] blocks = Blocks!.Array;
                byte[] _blocks = new byte[Blocks!.Array.Length * sizeof(ushort)];

                // fast copy
                fixed (byte* bytePtr = _blocks)
                fixed (ushort* ushortPtr = blocks)
                {
                    Buffer.MemoryCopy(ushortPtr, bytePtr, _blocks.Length, blocks.Length * sizeof(ushort));
                }

                writer.WriteBytes(_blocks);
            }

            if (HasSettings)
            {
                writer.WriteUInt16((ushort)Settings!.Count);

                for (int i = 0; i < Settings!.Count; i++)
                    Settings[i]!.Save(writer);
            }

            if (HasConnections)
            {
                writer.WriteUInt16((ushort)Connections!.Count);

                for (int i = 0; i < Connections!.Count; i++)
                    Connections[i]!.Save(writer);
            }
        }

        public static unsafe RawPrefab Load(FcBinaryReader reader)
        {
            byte header0 = reader.ReadUInt8();
            byte header1 = reader.ReadUInt8();

            bool hasConnections = (header0 & 1) == 1;
            bool hasSettings = ((header0 >> 1) & 1) == 1;
            bool hasBlocks = ((header0 >> 2) & 1) == 1;
            bool hasVoxels = ((header0 >> 3) & 1) == 1;
            bool isInGroup = ((header0 >> 4) & 1) == 1;
            bool hasColliderByte = ((header0 >> 5) & 1) == 1;
            bool unEditable = ((header0 >> 6) & 1) == 1;
            bool unEditable2 = (header0 >> 7) == 1;
            bool nonDefaultBackgroundColor = (header1 & 1) == 1;
            bool hasData2 = ((header1 >> 1) & 1) == 1;
            bool hasData1 = ((header1 >> 2) & 1) == 1;
            bool nonDefaultName = ((header1 >> 3) & 1) == 1;
            bool hasTypeByte = ((header1 >> 4) & 1) == 1;

            byte typeByte = 0;
            if (hasTypeByte)
                typeByte = reader.ReadUInt8();

            string name = "New Block";
            if (nonDefaultName)
                name = reader.ReadString();

            byte data1 = 0;
            if (hasData1)
                data1 = reader.ReadUInt8();

            uint data2 = 0;
            if (hasData2)
                data2 = reader.ReadUInt32();

            byte backgroundColor = (byte)FancadeColorE.Default;
            if (nonDefaultBackgroundColor)
                backgroundColor = reader.ReadUInt8();

            byte colliderByte = 0;
            if (hasColliderByte)
                colliderByte = reader.ReadUInt8();

            ushort groupId = 0;
            Vector3B posInGroup = default;
            if (isInGroup)
            {
                groupId = reader.ReadUInt16();
                posInGroup = reader.ReadVec3B();
            }

            byte[]? voxels = null;
            if (hasVoxels)
            {
                // size (8*8*8) * sides (6)
                voxels = reader.ReadBytes(8 * 8 * 8 * 6);
            }

            Vector3US insideSize = default;
            ushort[]? blocks = null;
            if (hasBlocks)
            {
                insideSize = reader.ReadVec3US();

                int insideLen = insideSize.X * insideSize.Y * insideSize.Z;

                if (insideLen == 0)
                    blocks = new ushort[0];
                else
                {
                    byte[] _blocks = reader.ReadBytes(insideLen * sizeof(ushort));

                    blocks = new ushort[insideLen];

                    // fast copy
                    fixed (byte* bytePtr = _blocks)
                    fixed (ushort* ushortPtr = blocks)
                    {
                        Buffer.MemoryCopy(bytePtr, ushortPtr, insideLen * sizeof(ushort), _blocks.Length);
                    }
                }
            }

            ushort numbSettings = 0;
            List<PrefabSetting>? settings = null;
            if (hasSettings)
            {
                numbSettings = reader.ReadUInt16();

                if (numbSettings == 0)
                    settings = new List<PrefabSetting>();
                else
                {
                    settings = new List<PrefabSetting>(numbSettings);

                    for (int i = 0; i < numbSettings; i++)
                        settings.Add(PrefabSetting.Load(reader));
                }
            }

            ushort numbConnections = 0;
            List<Connection>? connections = null;
            if (hasConnections)
            {
                numbConnections = reader.ReadUInt16();

                if (numbConnections == 0)
                    connections = new List<Connection>();
                else
                {
                    connections = new List<Connection>(numbConnections);

                    for (int i = 0; i < numbConnections; i++)
                        connections.Add(Connection.Load(reader));
                }
            }

            return new RawPrefab(hasConnections, hasSettings, hasBlocks, hasVoxels, isInGroup, hasColliderByte, unEditable, unEditable2, nonDefaultBackgroundColor, hasData2, hasData1, nonDefaultName, hasTypeByte, typeByte, name, data1, data2, backgroundColor, colliderByte, groupId, posInGroup, voxels, blocks is null ? null : new Array3D<ushort>(blocks, insideSize.X, insideSize.Y, insideSize.Z), settings, connections);
        }
    }
}