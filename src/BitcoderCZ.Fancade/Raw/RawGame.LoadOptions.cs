// <copyright file="RawGame.LoadOptions.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace BitcoderCZ.Fancade.Raw;

public partial class RawGame
{
    /// <summary>
    /// Determines which things should get loadded.
    /// </summary>
    [Flags]
    public enum LoadFlags
    {
        /// <summary>
        /// Load <see cref="RawGame.Prefabs"/>.
        /// </summary>
        Prefabs = 1 << 0,

        /// <summary>
        /// Load everything.
        /// </summary>
        All = int.MaxValue,
    }

    /// <summary>
    /// Options for <see cref="RawGame.Load(FcBinaryReader, LoadOptions?)"/> and <see cref="RawGame.LoadCompressed(Stream)"/>.
    /// </summary>
    public readonly struct LoadOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LoadOptions"/> struct.
        /// </summary>
        public LoadOptions()
        {
        }

        /// <summary>
        /// Gets the default load options.
        /// </summary>
        /// <value>The default load options.</value>
        public static LoadOptions Default => new LoadOptions();

        /// <summary>
        /// Gets the <see cref="LoadFlags"/>.
        /// </summary>
        /// <value>The <see cref="LoadFlags"/>, determines which things to load.</value>
        public readonly LoadFlags Flags { get; init; } = LoadFlags.All;

        /// <summary>
        /// Gets the prefab load options.
        /// </summary>
        /// <value>The load options for prefabs.</value>
        public RawPrefab.LoadOptions PrefabOptions { get; init; } = RawPrefab.LoadOptions.Default;
    }
}