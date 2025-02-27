// <copyright file="Game.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Raw;
using FancadeLoaderLib.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FancadeLoaderLib;

/// <summary>
/// Represents a fancade game, processed for easier manipulation.
/// </summary>
public class Game : ICloneable
{
	/// <summary>
	/// The prefabs of this game.
	/// </summary>
	public readonly PrefabList Prefabs;

	private string _name;

	private string _author;

	private string _description;

	/// <summary>
	/// Initializes a new instance of the <see cref="Game"/> class.
	/// </summary>
	/// <param name="name">The name of this game.</param>
	public Game(string name)
		: this(name, "Unknown Author", string.Empty, Enumerable.Empty<Prefab>())
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Game"/> class.
	/// </summary>
	/// <param name="name">The name of this game.</param>
	/// <param name="author">The name of the author of this game.</param>
	/// <param name="description">The description of this game.</param>
	/// <param name="prefabs">The prefabs of this game.</param>
	public Game(string name, string author, string description, IEnumerable<Prefab> prefabs)
		: this(name, author, description, [.. prefabs])
	{
	}

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable. - set by the properties
	/// <summary>
	/// Initializes a new instance of the <see cref="Game"/> class.
	/// </summary>
	/// <param name="name">The name of this game.</param>
	/// <param name="author">The name of the author of this game.</param>
	/// <param name="description">The description of this game.</param>
	/// <param name="prefabs">The prefabs of this game.</param>
	public Game(string name, string author, string description, PrefabList prefabs)
#pragma warning restore CS8618
	{
		if (prefabs is null)
		{
			throw new ArgumentNullException(nameof(prefabs));
		}

		Name = name;
		Author = author;
		Description = description;
		Prefabs = prefabs;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Game"/> class.
	/// </summary>
	/// <param name="other">The <see cref="Game"/> to copy values from.</param>
	/// <param name="deepCopy">If true, <see cref="Prefabs"/> will also be cloned.</param>
	public Game(Game other, bool deepCopy)
#pragma warning disable CA1062 // Validate arguments of public methods
		: this(other.Name, other.Author, other.Description, deepCopy ? other.Prefabs.Clone(true) : other.Prefabs)
#pragma warning restore CA1062 // Validate arguments of public methods
	{
	}

	/// <summary>
	/// Gets or sets the name of this game.
	/// </summary>
	/// <value>Name of this game.</value>
	public string Name
	{
		get => _name;
		set
		{
			if (value is null)
			{
				ThrowHelper.ThrowArgumentNull(nameof(value), $"{nameof(Name)} cannot be null.");
			}

			_name = value;
		}
	}

	/// <summary>
	/// Gets or sets the author of this game.
	/// </summary>
	/// <value>Author of this game.</value>
	public string Author
	{
		get => _author;
		set
		{
			if (value is null)
			{
				ThrowHelper.ThrowArgumentNull(nameof(value), $"{nameof(Author)} cannot be null.");
			}

			_author = value;
		}
	}

	/// <summary>
	/// Gets or sets the description of this game.
	/// </summary>
	/// <value>Description of this game.</value>
	public string Description
	{
		get => _description;
		set
		{
			if (value is null)
			{
				ThrowHelper.ThrowArgumentNull(nameof(value), $"{nameof(Description)} cannot be null.");
			}

			_description = value;
		}
	}

	/// <summary>
	/// Converts a <see cref="RawGame"/> into <see cref="Game"/>.
	/// </summary>
	/// <param name="game">The <see cref="RawGame"/> to create this <see cref="Game"/> from.</param>
	/// <param name="clonePrefabs">If the prefabs should be copied, if <see langword="true"/>, <paramref name="game"/> shouldn't be used anymore.</param>
	/// <returns>A new instance of the <see cref="Game"/> class from a <see cref="RawGame"/>.</returns>
	public static Game FromRaw(RawGame game, bool clonePrefabs = true)
	{
		if (game is null)
		{
			ThrowHelper.ThrowArgumentNull(nameof(game));
		}

		List<Prefab> prefabs = new List<Prefab>(game.Prefabs.Count);

		short idOffsetAddition = (short)(-game.IdOffset + RawGame.CurrentNumbStockPrefabs);

		for (int i = 0; i < game.Prefabs.Count; i++)
		{
			prefabs.Add(Prefab.FromRaw(game.Prefabs[i], game.IdOffset, idOffsetAddition, clonePrefabs));
		}

		return new Game(game.Name, game.Author, game.Description, prefabs);
	}

	/// <summary>
	/// Loads a game from a reader.
	/// </summary>
	/// <param name="reader">The reader to load from.</param>
	/// <returns>The loaded game.</returns>
	public static Game Load(FcBinaryReader reader)
		=> FromRaw(RawGame.Load(reader), false);

	/// <summary>
	/// Loads a game from a compressed stream.
	/// </summary>
	/// <param name="stream">The stream to load from.</param>
	/// <returns>The loaded game.</returns>
	public static Game LoadCompressed(Stream stream)
		=> FromRaw(RawGame.LoadCompressed(stream), false);

	/// <summary>
	/// Makes this game editable.
	/// </summary>
	/// <remarks>
	/// Sets <see cref="Prefab.Editable"/> of all prefabs in <see cref="Prefabs"/> to <see langword="true"/>.
	/// </remarks>
	/// <param name="changeAuthor">If <see langword="true"/>, <see cref="Author"/> is changed to "Unknown Author".</param>
	public void MakeEditable(bool changeAuthor)
	{
		if (changeAuthor)
		{
			Author = "Unknown Author";
		}

		for (int i = 0; i < Prefabs.Count; i++)
		{
			Prefabs[i].Editable = true;
		}
	}

	/// <summary>
	/// Calls <see cref="BlockData.Trim"/> on all prefabs in <see cref="Prefabs"/>.
	/// </summary>
	public void TrimPrefabs()
	{
		foreach (var prefab in Prefabs)
		{
			prefab.Blocks.Trim();
		}
	}

	/// <summary>
	/// Converts this <see cref="Game"/> into <see cref="RawGame"/>.
	/// </summary>
	/// <param name="clonePrefabs">If the prefabs should be copied, if <see langword="true"/>, this <see cref="Game"/> instance shouldn't be used anymore.</param>
	/// <returns>A new instance of the <see cref="RawGame"/> class from this <see cref="game"/>.</returns>
	public RawGame ToRaw(bool clonePrefabs)
	{
		List<RawPrefab> prefabs = new List<RawPrefab>(Prefabs.Count);

		for (int i = 0; i < Prefabs.Count; i++)
		{
			prefabs.Add(Prefabs[i].ToRaw(clonePrefabs));
		}

		return new RawGame(Name, Author, Description, RawGame.CurrentNumbStockPrefabs, prefabs);
	}

	/// <summary>
	/// Saves a game to a writer.
	/// </summary>
	/// <param name="writer">The writer to save to.</param>
	public void Save(FcBinaryWriter writer)
		=> ToRaw(false).Save(writer);

	/// <summary>
	/// Saves and compresses game to a stream.
	/// </summary>
	/// <param name="stream">The stream to save to.</param>
	public void SaveCompressed(Stream stream)
		=> ToRaw(false).SaveCompressed(stream);

	/// <summary>
	/// Creates a copy of this <see cref="Game"/>.
	/// </summary>
	/// <param name="deepCopy">If true, <see cref="Prefabs"/> will also be cloned.</param>
	/// <returns>A copy of this <see cref="Game"/>.</returns>
	public Game Clone(bool deepCopy)
		=> new Game(this, deepCopy);

	/// <inheritdoc/>
	object ICloneable.Clone()
		=> new Game(this, true);
}
