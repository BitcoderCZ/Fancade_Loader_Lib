// <copyright file="RawPrefab.LoadOptions.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace BitcoderCZ.Fancade.Raw;

public partial class RawPrefab
{
    /// <summary>
    /// Determines which things should get loadded.
    /// </summary>
    [Flags]
    public enum LoadFlags
    {
        /// <summary>
        /// Load <see cref="RawPrefab.Blocks"/>.
        /// </summary>
        Blocks = 1 << 0,

        /// <summary>
        /// Load <see cref="RawPrefab.Connections"/>.
        /// </summary>
        Connections = 1 << 1,

        /// <summary>
        /// Load <see cref="RawPrefab.Settings"/>.
        /// </summary>
        Settings = 1 << 2,

        /// <summary>
        /// Load <see cref="RawPrefab.Voxels"/>.
        /// </summary>
        Voxels = 1 << 3,

        /// <summary>
        /// Load everything.
        /// </summary>
        All = int.MaxValue,
    }

    /// <summary>
    /// Options for <see cref="RawPrefab.Load(FcBinaryReader, LoadOptions?)"/>.
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
    }
}