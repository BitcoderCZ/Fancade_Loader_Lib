// <copyright file="RawGame.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Utils;
using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib.Raw;

/// <summary>
/// Directly represents a fancade game.
/// </summary>
public class RawGame
{
    /// <summary>
    /// The current file version.
    /// </summary>
    public static readonly ushort CurrentFileVersion = 31;

    /// <summary>
    /// The current ammount of segments of stock/built in prefabs in fancade.
    /// </summary>
    public static readonly ushort CurrentNumbStockPrefabs = 597;

    /// <summary>
    /// The prefabs of this game.
    /// </summary>
#pragma warning disable CA1002 // Do not expose generic lists
    public readonly List<RawPrefab> Prefabs;
#pragma warning restore CA1002

    /// <summary>
    /// The name of this game.
    /// </summary>
    public string Name;

    /// <summary>
    /// Username of the author of this game.
    /// </summary>
    public string Author;

    /// <summary>
    /// The description of this game.
    /// </summary>
    public string Description;

    /// <summary>
    /// The id offset of prefabs in this game, specifies the amoung of stock prefabs at the time the game was save.
    /// </summary>
    /// <remarks>
    /// Not used when saving, <see cref="CurrentNumbStockPrefabs"/> is used instead.
    /// </remarks>
    public ushort IdOffset;

    /// <summary>
    /// Initializes a new instance of the <see cref="RawGame"/> class.
    /// </summary>
    /// <param name="name">Name of this game.</param>
    public RawGame(string name)
    {
        ThrowIfNull(name, nameof(name));

        Name = name;
        Author = "Unknown Author";
        Description = string.Empty;
        Prefabs = [];
        IdOffset = CurrentNumbStockPrefabs;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RawGame"/> class.
    /// </summary>
    /// <param name="name">Name of this game.</param>
    /// <param name="author">Username of the author of this game.</param>
    /// <param name="description">Decsription of this game.</param>
    /// <param name="idOffset">Id offset of this game.</param>
    /// <param name="prefabs">Prefabs of this game.</param>
    /// <exception cref="ArgumentNullException">Thrown when any of the arguments is null.</exception>
#pragma warning disable CA1002 // Do not expose generic lists
    public RawGame(string name, string author, string description, ushort idOffset, List<RawPrefab> prefabs)
#pragma warning restore CA1002
    {
        ThrowIfNull(name, nameof(name));
        ThrowIfNull(author, nameof(author));
        ThrowIfNull(description, nameof(description));
        ThrowIfNull(prefabs, nameof(prefabs));

        Name = name;
        Author = author;
        Description = description;
        IdOffset = idOffset;
        Prefabs = prefabs;
    }

    /// <summary>
    /// Loads the info about a game from a compressed stream.
    /// </summary>
    /// <param name="stream">The stream to load from.</param>
    /// <returns>The info about a game.</returns>
    public static (ushort FileVersion, string Name, string Author, string Description) LoadInfoCompressed(Stream stream)
    {
        // decompress
        using MemoryStream ms = new MemoryStream();

        Zlib.Decompress(stream, ms);
        ms.Position = 0;

        using FcBinaryReader reader = new FcBinaryReader(ms);

        ushort fileVersion = reader.ReadUInt16();

        string name = reader.ReadString();
        string author = reader.ReadString();
        string description = reader.ReadString();

        return (fileVersion, name, author, description);
    }

    /// <summary>
    /// Loads a <see cref="RawGame"/> from a zlib compressed <see cref="Stream"/>.
    /// </summary>
    /// <param name="stream">The reader to read the <see cref="RawGame"/> from.</param>
    /// <returns>A <see cref="RawGame"/> read from <paramref name="stream"/>.</returns>
    public static RawGame LoadCompressed(Stream stream)
    {
        // decompress
        using MemoryStream ms = new MemoryStream();

        Zlib.Decompress(stream, ms);
        ms.Position = 0;

        using FcBinaryReader reader = new FcBinaryReader(ms);

        return Load(reader);
    }

    /// <summary>
    /// Loads a <see cref="RawGame"/> from a <see cref="FcBinaryReader"/>.
    /// </summary>
    /// <param name="reader">The reader to read the <see cref="RawGame"/> from.</param>
    /// <returns>A <see cref="RawGame"/> read from <paramref name="reader"/>.</returns>
    public static RawGame Load(FcBinaryReader reader)
    {
        ThrowIfNull(reader, nameof(reader));

        ushort fileVersion = reader.ReadUInt16();

        if (fileVersion > CurrentFileVersion || fileVersion < 26)
        {
            ThrowUnsupportedVersionException(fileVersion);
        }
        else if (fileVersion == 26)
        {
            ThrowNotImplementedException("Loading file verison 26 has not yet been implemented.");
        }

        string name = reader.ReadString();
        string author = reader.ReadString();
        string description = reader.ReadString();

        ushort idOffset = reader.ReadUInt16();

        ushort numbPrefabs = reader.ReadUInt16();

        List<RawPrefab> prefabs = new List<RawPrefab>(numbPrefabs);
        for (int i = 0; i < numbPrefabs; i++)
        {
            prefabs.Add(RawPrefab.Load(reader));
        }

        return new RawGame(name, author, description, idOffset, prefabs);
    }

    /// <summary>
    /// Writes and compresses a <see cref="RawPrefab"/> into a <see cref="Stream"/>.
    /// </summary>
    /// <param name="stream">The <see cref="Stream"/> to write this instance into.</param>
    /// <param name="compressionLevel">
    /// Determines how much will the output be compressed.
    /// <para></para>
    /// <c>-1</c> for default compression; otherwise, <c>0</c> to <c>9</c>.
    /// </param>
    public void SaveCompressed(Stream stream, int compressionLevel = -1)
    {
        using (MemoryStream writerStream = new MemoryStream())
        using (FcBinaryWriter writer = new FcBinaryWriter(writerStream))
        {
            Save(writer);

            writerStream.Position = 0;
            Zlib.Compress(writerStream, stream, compressionLevel);
        }
    }

    /// <summary>
    /// Writes a <see cref="RawPrefab"/> into a <see cref="FcBinaryWriter"/>.
    /// </summary>
    /// <param name="writer">The <see cref="FcBinaryWriter"/> to write this instance into.</param>
    public void Save(FcBinaryWriter writer)
    {
        ThrowIfNull(writer, nameof(writer));

        writer.WriteUInt16(CurrentFileVersion);
        writer.WriteString(Name);
        writer.WriteString(Author);
        writer.WriteString(Description);
        writer.WriteUInt16(CurrentNumbStockPrefabs);

        writer.WriteUInt16((ushort)Prefabs.Count);

        for (int i = 0; i < Prefabs.Count; i++)
        {
            Prefabs[i].Save(writer);
        }
    }

    /// <summary>
    /// "Fixes" the order of prefabs in groups.
    /// </summary>
    /// <remarks>
    /// May change the execution order.
    /// </remarks>
    public void FixPrefabOrder()
    {
        PositionComparer comparer = PositionComparer.Instance;

        var prefabs = CollectionsMarshal.AsSpan(Prefabs);

        Dictionary<ushort, ushort> idsMap = [];
        Dictionary<ushort, int3> mainMoveMap = [];

        for (int i = 0; i < prefabs.Length; i++)
        {
            if (!prefabs[i].IsInGroup)
            {
                continue;
            }

            int startIndex = i;
            ushort groupId = prefabs[i].GroupId;

            bool fix = false;

            i++;
            while (i < prefabs.Length && prefabs[i].GroupId == groupId)
            {
                if (prefabs[i].HasMainInfo || comparer.Compare(prefabs[i - 1].PosInGroup, prefabs[i].PosInGroup) >= 0)
                {
                    fix = true;
                }

                i++;
            }

            if (fix)
            {
                FixPrefabOrder(prefabs[startIndex..i], (ushort)(startIndex + IdOffset), idsMap, mainMoveMap);
            }

            i--;
        }

        if (idsMap.Count == 0 && mainMoveMap.Count == 0)
        {
            return;
        }

        foreach (var prefab in prefabs)
        {
            var blocks = prefab.Blocks;

            if (blocks is not null)
            {
                for (int z = 0; z < blocks.Size.Z; z++)
                {
                    for (int y = 0; y < blocks.Size.Y; y++)
                    {
                        for (int x = 0; x < blocks.Size.X; x++)
                        {
                            int3 pos = new int3(x, y, z);
                            ushort id = blocks.GetUnchecked(pos);

                            if (id != 0)
                            {
                                if (idsMap.TryGetValue(id, out ushort newId))
                                {
                                    blocks.SetUnchecked(pos, newId);
                                    id = newId;
                                }

                                if (mainMoveMap.TryGetValue(id, out int3 move))
                                {
                                    if (prefab.Settings is not null)
                                    {
                                        for (int i = 0; i < prefab.Settings.Count; i++)
                                        {
                                            var setting = prefab.Settings[i];

                                            if (setting.Position == pos && setting.Type < SettingType.VoidTerminal)
                                            {
                                                prefab.Settings[i] = setting with { Position = (ushort3)(setting.Position + move) };
                                            }
                                        }
                                    }

                                    if (prefab.Connections is not null)
                                    {
                                        for (int i = 0; i < prefab.Connections.Count; i++)
                                        {
                                            var connection = prefab.Connections[i];

                                            if (!connection.IsFromOutside && connection.From == pos)
                                            {
                                                connection.From = (ushort3)(connection.From + move);
                                                prefab.Connections[i] = connection;
                                            }

                                            if (!connection.IsToOutside && connection.To == pos)
                                            {
                                                connection.To = (ushort3)(connection.To + move);
                                                prefab.Connections[i] = connection;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Returns the string representation of the current instance.
    /// </summary>
    /// <returns>The string representation of the current instance.</returns>
    public override string ToString()
        => $"{{Name: {Name}, Author: {Author}, Description: {Description}}}";

    private void FixPrefabOrder(Span<RawPrefab> prefabs, ushort groupId, Dictionary<ushort, ushort> idsMap, Dictionary<ushort, int3> mainMoveMap)
    {
        Debug.Assert(prefabs.Length > 1, $"{nameof(prefabs)} should be a group.");
        Debug.Assert(prefabs.Length < Prefab.MaxSize * Prefab.MaxSize * Prefab.MaxSize, $"{nameof(prefabs)}.Length should be less than max size.");

        Span<byte3> originalPositions = stackalloc byte3[prefabs.Length];

        for (int i = 0; i < prefabs.Length; i++)
        {
            originalPositions[i] = prefabs[i].PosInGroup;
            prefabs[i].GroupId = groupId;
        }

        prefabs.Sort((a, b) => PositionComparer.Instance.Compare(a.PosInGroup, b.PosInGroup));

        bool wasOutOfOrder = false;

        for (int i = 0; i < prefabs.Length; i++)
        {
            if (originalPositions[i] != prefabs[i].PosInGroup)
            {
                wasOutOfOrder = true;
                break;
            }
        }

        if (wasOutOfOrder)
        {
            Span<byte3> newPositions = stackalloc byte3[prefabs.Length];

            for (int i = 0; i < prefabs.Length; i++)
            {
                newPositions[i] = prefabs[i].PosInGroup;
            }

            for (int i = 0; i < prefabs.Length; i++)
            {
                int newIndex = newPositions.IndexOf(originalPositions[i]);
                Debug.Assert(newIndex != -1, $"All positions in {nameof(originalPositions)} should be in {nameof(newPositions)}.");

                if (newIndex != i)
                {
                    idsMap.Add((ushort)(groupId + i), (ushort)(groupId + newIndex));
                }
            }
        }

        for (int i = 1; i < prefabs.Length; i++)
        {
            if (prefabs[i].HasMainInfo)
            {
                mainMoveMap.Add((ushort)(groupId + i), -(int3)prefabs[i].PosInGroup);

                var prefab = prefabs[i];
                var mainPrefab = prefabs[0];

                mainPrefab.NonDefaultName = prefab.NonDefaultName;
                mainPrefab.Name = prefab.Name;
                mainPrefab.HasBlocks = prefab.HasBlocks;
                mainPrefab.Blocks = prefab.Blocks;
                mainPrefab.HasSettings = prefab.HasSettings;
                mainPrefab.Settings = prefab.Settings;
                mainPrefab.HasConnections = prefab.HasConnections;
                mainPrefab.Connections = prefab.Connections;

                prefab.NonDefaultName = false;
                prefab.Name = RawPrefab.DefaultName;
                prefab.HasBlocks = false;
                prefab.Blocks = null;
                prefab.HasSettings = false;
                prefab.Settings = null;
                prefab.HasConnections = false;
                prefab.Connections = null;

                break;
            }
        }
    }
}