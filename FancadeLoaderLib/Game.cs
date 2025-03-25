// <copyright file="Game.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Raw;
using FancadeLoaderLib.Utils;
using System;
using System.IO;
using System.Linq;
using System.Text;
using static FancadeLoaderLib.Utils.ThrowHelper;

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
        : this(name, "Unknown Author", string.Empty, new PrefabList())
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
            ThrowArgumentNullException(nameof(prefabs));
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
            if (string.IsNullOrEmpty(value))
            {
                ThrowArgumentException($"{nameof(Name)} cannot be null or empty.", nameof(value));
            }
            else if (Encoding.UTF8.GetByteCount(value) > 255)
            {
                ThrowArgumentException($"{nameof(Name)}, when UTF-8 encoded, cannot be longer than 255 bytes.", nameof(value));
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
            if (string.IsNullOrEmpty(value))
            {
                ThrowArgumentException($"{nameof(Author)} cannot be null or empty.", nameof(value));
            }
            else if (Encoding.UTF8.GetByteCount(value) > 255)
            {
                ThrowArgumentException($"{nameof(Author)}, when UTF-8 encoded, cannot be longer than 255 bytes.", nameof(value));
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
                ThrowArgumentNullException($"{nameof(Description)} cannot be null.", nameof(value));
            }
            else if (Encoding.UTF8.GetByteCount(value) > 255)
            {
                ThrowArgumentException($"{nameof(Description)}, when UTF-8 encoded, cannot be longer than 255 bytes.", nameof(value));
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
            ThrowArgumentNullException(nameof(game));
        }

        var prefabs = new PrefabList();

        short idOffsetAddition = (short)(-game.IdOffset + RawGame.CurrentNumbStockPrefabs);

        for (int i = 0; i < game.Prefabs.Count; i++)
        {
            if (game.Prefabs[i].IsInGroup)
            {
                int startIndex = i;
                ushort groupId = game.Prefabs[i].GroupId;
                do
                {
                    i++;
                } while (i < game.Prefabs.Count && game.Prefabs[i].GroupId == groupId);

                prefabs.AddPrefab(Prefab.FromRaw((ushort)(startIndex + RawGame.CurrentNumbStockPrefabs), game.Prefabs.Skip(startIndex).Take(i - startIndex), game.IdOffset, idOffsetAddition, clonePrefabs));

                i--; // incremented at the end of the loop
            }
            else
            {
                prefabs.AddPrefab(Prefab.FromRaw((ushort)(i + RawGame.CurrentNumbStockPrefabs), [game.Prefabs[i]], game.IdOffset, idOffsetAddition, clonePrefabs));
            }
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
    /// Sets <see cref="PrefabSegment.Editable"/> of all prefabs in <see cref="Prefabs"/> to <see langword="true"/>.
    /// </remarks>
    /// <param name="changeAuthor">If <see langword="true"/>, <see cref="Author"/> is changed to "Unknown Author"; otherwise only <see cref="Prefab.Editable"/> is set to <see langword="true"/>.</param>
    public void MakeEditable(bool changeAuthor)
    {
        if (changeAuthor)
        {
            Author = "Unknown Author";
        }

        foreach (var prefab in Prefabs.Prefabs)
        {
            prefab.Editable = true;
        }
    }

    /// <summary>
    /// Calls <see cref="BlockData.Trim"/> on all prefabs in <see cref="Prefabs"/>.
    /// </summary>
    public void TrimPrefabs()
    {
        foreach (var prefab in Prefabs.Prefabs)
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
        => new RawGame(Name, Author, Description, RawGame.CurrentNumbStockPrefabs, [.. Prefabs.Prefabs.OrderBy(item => item.Id).SelectMany(item => item.ToRaw(clonePrefabs)).ToList()]);

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
