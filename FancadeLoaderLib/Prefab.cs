using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;

namespace FancadeLoaderLib
{
    public class Prefab : ICloneable
    {
        public const int NumbVoxels = 8 * 8 * 8;
        public static ImmutableArray<Vector3S> SideToOffset =
            new Vector3S[6]
            {
                new Vector3S(1, 0, 0),
                new Vector3S(-1, 0, 0),
                new Vector3S(0, 1, 0),
                new Vector3S(0, -1, 0),
                new Vector3S(0, 0, 1),
                new Vector3S(0, 0, -1),
            }.ToImmutableArray();

        private string name;
        public string Name
        {
            get => name;
            set
            {
                if (value is null)
                    throw new ArgumentNullException(nameof(value), $"{nameof(Name)} cannot be null.");

                name = value;
            }
        }

        public PrefabCollider Collider;
        public PrefabType Type;
        public FcColor BackgroundColor;

        public bool Editable;

        public bool IsInGourp => GroupId != ushort.MaxValue;
        public ushort GroupId;
        public Vector3B PosInGroup;

        private Voxel[]? voxels;
        public Voxel[]? Voxels
        {
            get => voxels;
            set
            {
                if (!(value is null) && value.Length != NumbVoxels)
                    throw new ArgumentException($"{nameof(Voxels)} must be {NumbVoxels} long, but {nameof(value)}.Length is {value.Length}.", nameof(value));

                voxels = value;
            }
        }
        public readonly BlockData Blocks;
        public readonly List<PrefabSetting> Settings;
        public readonly List<Connection> Connections;

        public Prefab()
        {
            name = "New Block";
            BackgroundColor = FcColorE.Default;
            Collider = PrefabCollider.Box;
            Type = PrefabType.Normal;
            Editable = true;
            GroupId = ushort.MaxValue;

            Blocks = new BlockData();
            Settings = new List<PrefabSetting>();
            Connections = new List<Connection>();
        }

        public Prefab(string name, PrefabCollider collider, PrefabType type, FcColor backgroundColor, bool editable, Voxel[]? voxels, BlockData? blocks, List<PrefabSetting>? settings, List<Connection>? connections)
            : this(name, collider, type, backgroundColor, editable, ushort.MaxValue, default, voxels, blocks, settings, connections)
        {
        }
        public Prefab(string name, PrefabCollider collider, PrefabType type, FcColor backgroundColor, bool editable, ushort groupId, Vector3B posInGroup, Voxel[]? voxels, BlockData? blocks, List<PrefabSetting>? settings, List<Connection>? connections)
        {
            this.name = name;
            Collider = collider;
            Type = type;
            BackgroundColor = backgroundColor;
            Editable = editable;
            GroupId = groupId;
            PosInGroup = posInGroup;
            Voxels = voxels;
            Blocks = blocks ?? new BlockData();
            Settings = settings ?? new List<PrefabSetting>();
            Connections = connections ?? new List<Connection>();
        }

        public Prefab(Prefab prefab)
            : this(prefab.name, prefab.Collider, prefab.Type, prefab.BackgroundColor, prefab.Editable, prefab.GroupId, prefab.PosInGroup, prefab.Voxels is null ? null : (Voxel[])prefab.Voxels.Clone(), prefab.Blocks.Clone(), new List<PrefabSetting>(prefab.Settings), new List<Connection>(prefab.Connections))
        {
        }

        public static Prefab CreateBlock(string name)
        {
            Prefab prefab = new Prefab();

            prefab.Name = name;
            prefab.voxels = new Voxel[NumbVoxels];

            return prefab;
        }
        public static Prefab CreateLevel(string name)
        {
            Prefab prefab = new Prefab();

            prefab.Name = name;
            prefab.Collider = PrefabCollider.None;
            prefab.Type = PrefabType.Level;

            return prefab;
        }

        public unsafe RawPrefab ToRaw(bool clone)
        {
            byte[]? voxels = null;
            if (!(Voxels is null))
            {
                voxels = new byte[NumbVoxels * 6];

                for (int i = 0; i < NumbVoxels; i++)
                {
                    Voxel voxel = Voxels[i];
                    voxels[i + NumbVoxels * 0] = (byte)(voxel.Colors[0] | voxel.Attribs[0] << 6);
                    voxels[i + NumbVoxels * 1] = (byte)(voxel.Colors[1] | voxel.Attribs[1] << 6);
                    voxels[i + NumbVoxels * 2] = (byte)(voxel.Colors[2] | voxel.Attribs[2] << 6);
                    voxels[i + NumbVoxels * 3] = (byte)(voxel.Colors[3] | voxel.Attribs[3] << 6);
                    voxels[i + NumbVoxels * 4] = (byte)(voxel.Colors[4] | voxel.Attribs[4] << 6);
                    voxels[i + NumbVoxels * 5] = (byte)(voxel.Colors[5] | voxel.Attribs[5] << 6);
                }
            }

            return new RawPrefab(hasConnections: !(Connections is null) && Connections.Count > 0, hasSettings: !(Settings is null) && Settings.Count > 0, hasBlocks: !(Blocks is null) && Blocks.Length > 0, hasVoxels: Type != PrefabType.Level && !(Voxels is null), isInGroup: GroupId != ushort.MaxValue, hasColliderByte: Collider != PrefabCollider.Box, unEditable: !Editable, unEditable2: !Editable, nonDefaultBackgroundColor: BackgroundColor != FcColorE.Default, hasData2: false, hasData1: false, Name != "New Block", hasTypeByte: Type != 0, typeByte: (byte)Type, name: Name, data1: 0, data2: 0, backgroundColor: (byte)BackgroundColor, colliderByte: (byte)Collider, groupId: GroupId, posInGroup: PosInGroup,
                voxels: voxels,
                blocks: Blocks is null ? null : (clone ? Blocks.Array.Clone() : Blocks.Array),
                settings: clone ? new List<PrefabSetting>(Settings) : Settings,
                connections: clone ? new List<Connection>(Connections) : Connections
            );
        }

        /// <summary>
        /// Converts <see cref="RawPrefab"/> into <see cref="Prefab"/>
        /// </summary>
        /// <param name="rawPrefab">The <see cref="RawPrefab"/> to convert</param>
        /// <param name="idOffset"></param>
        /// <param name="idOffsetAddition"></param>
        /// <param name="clone">If true clones Blocks, Settings and Connections else the values are assigned directly and <paramref name="rawPrefab"/> shouldn't be used anymore</param>
        /// <returns>The converted <see cref="Prefab"/></returns>
        /// <exception cref="ArgumentException"></exception>
        public static unsafe Prefab FromRaw(RawPrefab rawPrefab, ushort idOffset, short idOffsetAddition, bool clone = true)
        {
            PrefabType type = PrefabType.Normal;
            if (rawPrefab.HasTypeByte)
                type = (PrefabType)rawPrefab.TypeByte;

            string name = "New Block";
            if (rawPrefab.NonDefaultName)
                name = rawPrefab.Name;

            FcColor backgroundColor = FcColorE.Default;
            if (rawPrefab.NonDefaultBackgroundColor)
                backgroundColor = (FcColor)rawPrefab.BackgroundColor;

            bool editable = !rawPrefab.UnEditable && !rawPrefab.UnEditable2;

            PrefabCollider collider = PrefabCollider.Box;
            if (rawPrefab.HasColliderByte)
                collider = (PrefabCollider)rawPrefab.ColliderByte;

            ushort groupId = ushort.MaxValue;
            Vector3B posInGroup = default;
            if (rawPrefab.IsInGroup)
            {
                groupId = rawPrefab.GroupId;

                if (idOffset <= rawPrefab.GroupId)
                    groupId = (ushort)(groupId + idOffsetAddition);

                posInGroup = rawPrefab.PosInGroup;
            }

            Voxel[]? voxels = null;
            if (rawPrefab.HasVoxels)
            {
                if (rawPrefab.Voxels is null)
                    throw new ArgumentException($"{nameof(rawPrefab)}.{nameof(RawPrefab.HasVoxels)} is true, while {nameof(rawPrefab)}.{nameof(RawPrefab.Voxels)} is null", nameof(rawPrefab));

                voxels = new Voxel[NumbVoxels];

                for (int i = 0; i < voxels.Length; i++)
                {
                    Voxel voxel = new Voxel();
                    byte s0 = rawPrefab.Voxels[i + NumbVoxels * 0];
                    byte s1 = rawPrefab.Voxels[i + NumbVoxels * 1];
                    byte s2 = rawPrefab.Voxels[i + NumbVoxels * 2];
                    byte s3 = rawPrefab.Voxels[i + NumbVoxels * 3];
                    byte s4 = rawPrefab.Voxels[i + NumbVoxels * 4];
                    byte s5 = rawPrefab.Voxels[i + NumbVoxels * 5];

                    voxel.Colors[0] = (byte)(s0 & 0b_0011_1111);
                    voxel.Colors[1] = (byte)(s1 & 0b_0011_1111);
                    voxel.Colors[2] = (byte)(s2 & 0b_0011_1111);
                    voxel.Colors[3] = (byte)(s3 & 0b_0011_1111);
                    voxel.Colors[4] = (byte)(s4 & 0b_0011_1111);
                    voxel.Colors[5] = (byte)(s5 & 0b_0011_1111);
                    voxel.Attribs[0] = (byte)((s0 & 0b_1100_0000) >> 6);
                    voxel.Attribs[1] = (byte)((s1 & 0b_1100_0000) >> 6);
                    voxel.Attribs[2] = (byte)((s2 & 0b_1100_0000) >> 6);
                    voxel.Attribs[3] = (byte)((s3 & 0b_1100_0000) >> 6);
                    voxel.Attribs[4] = (byte)((s4 & 0b_1100_0000) >> 6);
                    voxel.Attribs[5] = (byte)((s5 & 0b_1100_0000) >> 6);

                    voxels[i] = voxel;
                }
            }

            BlockData? blocks = null;
            if (rawPrefab.HasBlocks)
            {
                if (rawPrefab.Blocks is null)
                    throw new ArgumentException($"{nameof(rawPrefab)}.{nameof(RawPrefab.HasBlocks)} is true, while {nameof(rawPrefab)}.{nameof(RawPrefab.Blocks)} is null", nameof(rawPrefab));

                ushort[] _blocks;
                if (clone)
                    _blocks = (ushort[])rawPrefab.Blocks.Array.Clone();
                else
                    _blocks = rawPrefab.Blocks.Array;

                for (int i = 0; i < _blocks.Length; i++)
                {
                    if (idOffset <= _blocks[i])
                        _blocks[i] = (ushort)(_blocks[i] + idOffsetAddition);
                }

                blocks = new BlockData(new Array3D<ushort>(_blocks, rawPrefab.Blocks.LengthX, rawPrefab.Blocks.LengthY, rawPrefab.Blocks.LengthZ));
            }

            List<PrefabSetting>? settings = null;
            if (rawPrefab.HasSettings)
            {
                if (rawPrefab.Settings is null)
                    throw new ArgumentException($"{nameof(rawPrefab)}.{nameof(RawPrefab.HasSettings)} is true, while {nameof(rawPrefab)}.{nameof(RawPrefab.Settings)} is null", nameof(rawPrefab));

                if (clone)
                    settings = new List<PrefabSetting>(rawPrefab.Settings);
                else
                    settings = rawPrefab.Settings;
            }

            // add settings to stock prefabs
            if (!(blocks is null) && !(settings is null) && blocks.Length != 0)
            {
                for (int i = 0; i < blocks.Length; i++)
                {
                    int id = blocks[i];

                    if (id != 0)
                    {
                        int numbStockSettings = 0; // TODO: getNumbStockSettings(id);
                        if (numbStockSettings != 0)
                        {
                            for (int setI = 0; setI < numbStockSettings; setI++)
                            {
                                Vector3US pos = (Vector3US)blocks.Index(i);

                                try
                                {
                                    PrefabSetting setting = settings.First(s => s.Index == setI && s.Position == pos);
                                }
                                catch
                                {
                                    // Wasn't found
                                    // TODO: settings.Add(getStockSetting(id, setI));
                                }
                            }
                        }
                    }
                }
            }

            List<Connection>? connections = null;
            if (rawPrefab.HasConnections)
            {
                if (rawPrefab.Connections is null)
                    throw new ArgumentException($"{nameof(rawPrefab)}.{nameof(RawPrefab.HasConnections)} is true, while {nameof(rawPrefab)}.{nameof(RawPrefab.Connections)} is null", nameof(rawPrefab));

                if (clone)
                    connections = new List<Connection>(rawPrefab.Connections);
                else
                    connections = rawPrefab.Connections;
            }

            return new Prefab(name, collider, type, backgroundColor, editable, groupId, posInGroup, voxels, blocks, settings, connections);
        }

        public Prefab Clone()
            => new Prefab(this);
        object ICloneable.Clone()
            => new Prefab(this);
    }

    public enum PrefabCollider : byte
    {
        None = 0,
        Box = 1,
        Sphere = 2,
    }

    public enum PrefabType : byte
    {
        Normal = 0,
        Physics = 1,
        Script = 2,
        Level = 3,
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct Voxel
    {
        //  X
        // -X
        //  Y
        // -Y
        //  Z
        // -Z
        public fixed byte Colors[6];
        public fixed byte Attribs[6]; // "legos"/glue

        public bool IsEmpty => Colors[0] == 0;

        public override string ToString() =>
            $"[{Colors[0]}, {Colors[1]}, {Colors[2]}, {Colors[3]}, {Colors[4]}, {Colors[5]}; Attribs:" +
            $"{Attribs[0]}, {Attribs[1]}, {Attribs[2]}, {Attribs[3]}, {Attribs[4]}, {Attribs[5]}]";
    }
}
