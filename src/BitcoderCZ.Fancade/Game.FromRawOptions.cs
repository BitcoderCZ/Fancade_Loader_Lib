// <copyright file="Game.FromRawOptions.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace BitcoderCZ.Fancade;

public partial class Game
{
    /// <summary>
    /// Options for <see cref="Game.FromRaw(Raw.RawGame, FromRawOptions?)"/>.
    /// </summary>
    public readonly struct FromRawOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FromRawOptions"/> struct.
        /// </summary>
        public FromRawOptions()
        {
        }

        /// <summary>
        /// Gets the default options.
        /// </summary>
        /// <value>The default options.</value>
        public static FromRawOptions Default => new FromRawOptions();

        /// <summary>
        /// Gets the prefab from raw options.
        /// </summary>
        /// <value>The from raw options for prefabs.</value>
        public Prefab.FromRawOptions PrefabOptions { get; init; } = Prefab.FromRawOptions.Default;
    }
}
