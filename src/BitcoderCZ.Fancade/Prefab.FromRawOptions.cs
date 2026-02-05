// <copyright file="Prefab.FromRawOptions.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Fancade.Data;

namespace BitcoderCZ.Fancade;

public sealed partial class Prefab
{
    /// <summary>
    /// Produces a <see cref="IBlockData"/> instance.
    /// </summary>
    /// <param name="blocks">Blocks of the prefab.</param>
    /// <param name="prefabId">Id of the prefab.</param>
    /// <param name="prefabType">Type of the prefab.</param>
    /// <remarks>
    /// The <see cref="Array3D{T}"/> will not be used after the call, it can be transfered to the <see cref="IBlockData"/>.
    /// </remarks>
    /// <returns>The <see cref="IBlockData"/> instance.</returns>
    public delegate IBlockData BlockDataFactory(Array3D<ushort> blocks, ushort prefabId, PrefabType prefabType);

    /// <summary>
    /// Options for <see cref="Prefab.FromRaw(ushort, IEnumerable{Raw.RawPrefab}, ushort, short, FromRawOptions?)"/>.
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
        /// Gets a value indicating whether blocks, settings and connections should be cloned.
        /// </summary>
        /// <remarks>
        /// <see langword="true"/> by default.
        /// </remarks>
        /// <value>If <see langword="true"/> clones Blocks, Settings and Connections; otherwise, the values are assigned directly and the raw prefabs shouldn't be used anymore.</value>
        public bool Clone {get; init;} = true;

        /// <summary>
        /// Gets a factory method that, given a <see cref="Array3D{T}"/> creates a <see cref="IBlockData"/> instance.
        /// </summary>
        /// <remarks>
        /// Produces <see cref="ArrayBlockData"/> by default.
        /// The <see cref="Array3D{T}"/> will not be used after the call, it can be transfered to the <see cref="IBlockData"/>.
        /// </remarks>
        /// <value>The <see cref="IBlockData"/> factory method.</value>
        public BlockDataFactory BlockDataFactory { get; init; } = static (array, _, _) => new ArrayBlockData(array);
    }
}
