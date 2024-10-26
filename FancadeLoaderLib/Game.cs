using FancadeLoaderLib.Raw;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FancadeLoaderLib
{
    public class Game : ICloneable
    {
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
        private string author;
        public string Author
        {
            get => author;
            set
            {
                if (value is null)
                    throw new ArgumentNullException(nameof(value), $"{nameof(Author)} cannot be null.");

                author = value;
            }
        }
        private string description;
        public string Description
        {
            get => description;
            set
            {
                if (value is null)
                    throw new ArgumentNullException(nameof(value), $"{nameof(Description)} cannot be null.");

                description = value;
            }
        }

        public readonly PrefabList Prefabs;

        public Game(string name)
            : this(name, "Unknown Author", string.Empty, Enumerable.Empty<Prefab>())
        {
        }
        public Game(string name, string author, string description, IEnumerable<Prefab> prefabs)
            : this(name, author, description, new PrefabList(prefabs))
        {
        }
        public Game(string name, string author, string description, PrefabList prefabs)
        {
            if (prefabs is null)
                throw new ArgumentNullException(nameof(prefabs));

            Name = name;
            Author = author;
            Description = description;
            Prefabs = prefabs;
        }
        public Game(Game game, bool deepCopy)
            : this(game.Name, game.Author, game.Description, deepCopy ? game.Prefabs.Clone(true) : game.Prefabs)
        {
        }

        public void MakeEditable(bool changeAuthor)
        {
            if (changeAuthor)
                Author = "Unknown Author";

            for (int i = 0; i < Prefabs.Count; i++)
                Prefabs[i].Editable = true;
        }

        public void TrimPrefabs()
        {
            foreach (var prefab in Prefabs)
                prefab.Blocks.Trim();
        }

        public RawGame ToRaw(bool clonePrefabs)
        {
            List<RawPrefab> prefabs = new List<RawPrefab>(Prefabs.Count);

            for (int i = 0; i < Prefabs.Count; i++)
                prefabs.Add(Prefabs[i].ToRaw(clonePrefabs));

            return new RawGame(Name, Author, Description, RawGame.CurrentNumbStockPrefabs, prefabs);
        }

        public static Game FromRaw(RawGame game, bool clonePrefabs = true)
        {
            List<Prefab> prefabs = new List<Prefab>(game.Prefabs.Count);

            short idOffsetAddition = (short)(-game.IdOffset + RawGame.CurrentNumbStockPrefabs);

            for (int i = 0; i < game.Prefabs.Count; i++)
                prefabs.Add(Prefab.FromRaw(game.Prefabs[i], game.IdOffset, idOffsetAddition, clonePrefabs));

            return new Game(game.Name, game.Author, game.Description, prefabs);
        }

        public void Save(FcBinaryWriter writer)
            => ToRaw(false).Save(writer);
        public void SaveCompressed(Stream stream)
            => ToRaw(false).SaveCompressed(stream);

        public static Game Load(FcBinaryReader reader)
            => FromRaw(RawGame.Load(reader), false);
        public static Game LoadCompressed(Stream stream)
            => FromRaw(RawGame.LoadCompressed(stream), false);

        public Game Clone(bool deepCopy)
            => new Game(this, deepCopy);
        object ICloneable.Clone()
            => new Game(this, true);
    }
}
