// <copyright file="RawGame.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Utils;
using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
    /// The current amount of stock/built in prefabs in fancade.
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
    /// The id offset of prefabs in this game, specifies the amount of stock prefabs <b>at the time the game was save</b>.
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
    /// <param name="description">Description of this game.</param>
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
            ThrowNotImplementedException("Loading file version 26 has not yet been implemented.");
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

        List<(int Index, RawPrefab Prefab)> prefabWithOgIndex = new(Prefabs.Count);

        Dictionary<ushort, int3> mainMoveMap = [];

        int randGroupId = -1; // makes sure prefabs not in group don't get grouped together
        foreach (var group in Prefabs.Select((prefab, index) => (index, prefab)).GroupBy(item => item.prefab.IsInGroup ? item.prefab.GroupId : randGroupId--))
        {
            if (group.Key < 0)
            {
                prefabWithOgIndex.Add(group.First());
                continue;
            }

            RawPrefab? firstPrefab = null;
            bool foundMain = false;
            foreach (var item in group.OrderBy(item => item.prefab.PosInGroup, PositionComparer.Instance))
            {
                var prefab = item.prefab;

                if (firstPrefab is null)
                {
                    firstPrefab = prefab;
                }
                else if (item.prefab.HasMainInfo)
                {
                    // wont happen in games from editor, but may happen if manually edited
                    if (!foundMain)
                    {
                        mainMoveMap.Add((ushort)(item.index + IdOffset), -(int3)prefab.PosInGroup);
                        foundMain = true;

                        firstPrefab.Name = prefab.Name;
                        firstPrefab.Blocks = prefab.Blocks;
                        firstPrefab.Settings = prefab.Settings;
                        firstPrefab.Connections = prefab.Connections;
                    }

                    prefab.Name = RawPrefab.DefaultName;
                    prefab.Blocks = null;
                    prefab.Settings = null;
                    prefab.Connections = null;
                }

                prefabWithOgIndex.Add(item);
            }
        }

        Dictionary<ushort, ushort> idsMap = [];
        for (int i = 0; i < prefabWithOgIndex.Count; i++)
        {
            var (ogIndex, prefab) = prefabWithOgIndex[i];

            idsMap.Add((ushort)(ogIndex + IdOffset), (ushort)(i + IdOffset)); 
        }

        // update group ids
        Dictionary<ushort, ushort> groupIdUpdates = [];
        for (int i = 0; i < prefabs.Length; i++)
        {
            ushort groupId = prefabs[i].GroupId;
            if (groupId != 0)
            {
                groupIdUpdates.TryAdd(groupId, (ushort)(i + IdOffset));
            }
        }

        foreach (var prefab in prefabs)
        {
            if (prefab.GroupId != 0)
            {
                prefab.GroupId = groupIdUpdates[prefab.GroupId];
            }
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

                                if (idsMap.TryGetValue(id, out ushort newId))
                                {
                                    blocks.SetUnchecked(pos, newId);
                                    id = newId;
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
}