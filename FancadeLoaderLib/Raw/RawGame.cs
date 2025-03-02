// <copyright file="RawGame.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Utils;
using System;
using System.Collections.Generic;
using System.IO;

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
	/// The current ammount of stock/built in prefabs in fancade.
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
		if (name is null)
		{
			ThrowHelper.ThrowArgumentNullException(nameof(name));
		}

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
		if (name is null)
		{
			ThrowHelper.ThrowArgumentNullException(nameof(name));
		}

		if (author is null)
		{
			ThrowHelper.ThrowArgumentNullException(nameof(author));
		}

		if (description is null)
		{
			ThrowHelper.ThrowArgumentNullException(nameof(description));
		}

		if (prefabs is null)
		{
			ThrowHelper.ThrowArgumentNullException(nameof(prefabs));
		}

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
	/// Loads a game from a compressed stream.
	/// </summary>
	/// <param name="stream">The stream to load from.</param>
	/// <returns>The loaded game.</returns>
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
	/// Loads a game from a reader.
	/// </summary>
	/// <param name="reader">The reader to load from.</param>
	/// <returns>The loaded game.</returns>
	public static RawGame Load(FcBinaryReader reader)
	{
		if (reader is null)
		{
			ThrowHelper.ThrowArgumentNullException(nameof(reader));
		}

		ushort fileVersion = reader.ReadUInt16();

		if (fileVersion > CurrentFileVersion || fileVersion < 26)
		{
			ThrowHelper.ThrowUnsupportedVersionException(fileVersion);
		}
		else if (fileVersion == 26)
		{
			ThrowHelper.ThrowNotImplementedException("Loading file verison 26 has not yet been implemented.");
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
	/// Saves and compresses game to a stream.
	/// </summary>
	/// <param name="stream">The stream to save to.</param>
	public void SaveCompressed(Stream stream)
	{
		using (MemoryStream writerStream = new MemoryStream())
		using (FcBinaryWriter writer = new FcBinaryWriter(writerStream))
		{
			Save(writer);

			writerStream.Position = 0;
			Zlib.Compress(writerStream, stream);
		}
	}

	/// <summary>
	/// Saves a game to a writer.
	/// </summary>
	/// <param name="writer">The writer to save to.</param>
	public void Save(FcBinaryWriter writer)
	{
		if (writer is null)
		{
			ThrowHelper.ThrowArgumentNullException(nameof(writer));
		}

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
	/// Returns the string representation of the current instance.
	/// </summary>
	/// <returns>The string representation of the current instance.</returns>
	public override string ToString()
		=> $"{{Name: {Name}, Author: {Author}, Description: {Description}}}";
}
