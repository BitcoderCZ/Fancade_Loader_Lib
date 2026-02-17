// <copyright file="PositionedNode.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BitcoderCZ.Maths.Vectors;
using SettingsCollection = BitcoderCZ.Buffers.InlineList<BitcoderCZ.Buffers.FixedArray2<BitcoderCZ.Fancade.PrefabSetting>, BitcoderCZ.Fancade.PrefabSetting>;

namespace BitcoderCZ.Fancade.Editing.Scripting;

/// <summary>
/// Represents a node in a <see cref="PositionedCodeGraph"/>.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct PositionedNode : IEquatable<PositionedNode>
{
    /// <summary>
    /// Gets the empty node.
    /// </summary>
    /// <remarks>
    /// Can be used for padding.
    /// </remarks>
    /// <value>The empty node.</value>
    public static PositionedNode Empty { get; } = new PositionedNode(0, 0, new BlockDef("Empty", ushort.MaxValue, ScriptBlockType.NonScript, PrefabType.Normal, int3.One, TerminalBuilder.Empty), default);

    private readonly ushort _graphId;
    private readonly ushort _index;
    private readonly BlockDef _type;
    private readonly short3 _offset;

    internal PositionedNode(ushort graphId, ushort index, BlockDef type, short3 offset)
    {
        _graphId = graphId;
        _index = index;
        _type = type;
        _offset = offset;
    }

    /// <summary>
    /// Gets a handle that uniquely identifies this node.
    /// </summary>
    /// <value>A handle that uniquely identifies this node.</value>
    public NodeHandle Handle => new NodeHandle(_graphId, _index);

    /// <summary>
    /// Gets the type of the node.
    /// </summary>
    /// <value>Type of the node.</value>
    public BlockDef Type => _type;

    /// <summary>
    /// Gets the position offset of the node.
    /// </summary>
    /// <value>The node's offset within the graph.</value>
    public int3 Offset => _offset;
    
      /// <summary>
    /// Gets a value indicating whether the <see cref="PositionedNode"/> is empty/air.
    /// </summary>
    /// <value><see langword="true"/> if the <see cref="PositionedNode"/> is empty; otherwise, <see langword="false"/>.</value>
    public bool IsEmpty => Handle == NodeHandle.Null;

    /// <summary>Returns a value that indicates whether the 2 <see cref="PositionedNode"/>s are equal.</summary>
    /// <param name="left">The first <see cref="PositionedNode"/> to compare.</param>
    /// <param name="right">The second <see cref="PositionedNode"/> to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(PositionedNode left, PositionedNode right)
        => left.Equals(right);

    /// <summary>Returns a value that indicates whether the 2 <see cref="PositionedNode"/>s are not equal.</summary>
    /// <param name="left">The first <see cref="PositionedNode"/> to compare.</param>
    /// <param name="right">The second <see cref="PositionedNode"/> to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(PositionedNode left, PositionedNode right)
        => !(left == right);

    /// <inheritdoc/>
    public bool Equals(PositionedNode other)
        => Handle == other.Handle;

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj is PositionedNode other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
        => Handle.GetHashCode();

    /// <summary>
    /// Represents a region of non-script blocks that can be constructed and connected to script nodes.
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    public readonly struct BlockRegion : IEquatable<BlockRegion>
    {
        internal readonly ushort _graphId;
        internal readonly ushort _index;
        internal readonly Array3D<ushort> _blocks;
        internal readonly int3 _offset;

        internal BlockRegion(ushort graphId, ushort index, Array3D<ushort> blocks, int3 offset)
        {
            _graphId = graphId;
            _index = index;
            _blocks = blocks;
            _offset = offset;
        }

        /// <summary>
        /// Gets a handle that uniquely identifies this region.
        /// </summary>
        /// <value>A handle that uniquely identifies this region.</value>
        public BlockRegionHandle Handle => new BlockRegionHandle(_graphId, _index);

        /// <summary>
        /// Gets the blocks contained in this region.
        /// </summary>
        /// <value>A 3D array indexed using region-relative block coordinates.</value>
        public IReadOnly3DArray<ushort> Blocks => _blocks;

        /// <summary>
        /// Gets the position offset of the range.
        /// </summary>
        /// <value>The range's offset within the graph.</value>
        public int3 Offset => _offset;

        /// <summary>Returns a value that indicates whether the 2 <see cref="BlockRegion"/>s are equal.</summary>
        /// <param name="left">The first <see cref="BlockRegion"/> to compare.</param>
        /// <param name="right">The second <see cref="BlockRegion"/> to compare.</param>
        /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are equal; otherwise, <see langword="false"/>.</returns>
        public static bool operator ==(BlockRegion left, BlockRegion right)
            => left.Equals(right);

        /// <summary>Returns a value that indicates whether the 2 <see cref="BlockRegion"/>s are not equal.</summary>
        /// <param name="left">The first <see cref="BlockRegion"/> to compare.</param>
        /// <param name="right">The second <see cref="BlockRegion"/> to compare.</param>
        /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are not equal; otherwise, <see langword="false"/>.</returns>
        public static bool operator !=(BlockRegion left, BlockRegion right)
            => !(left == right);

        /// <inheritdoc/>
        public bool Equals(BlockRegion other)
            => Handle == other.Handle;

        /// <inheritdoc/>
        public override bool Equals(object? obj)
            => obj is BlockRegion other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode()
            => Handle.GetHashCode();
    }
}

/// <summary>
/// Represents a <see cref="PositionedNode"/>, without the handle.
/// </summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct PositionedNodeData
{
    internal readonly BlockDef _type;
    internal readonly SettingsCollection _settings;
    internal readonly short3 _offset;

    /// <summary>
    /// Initializes a new instance of the <see cref="PositionedNodeData"/> struct.
    /// </summary>
    /// <param name="type">Type of the node.</param>
    /// <param name="settings">Settings of the node.</param>
    /// <param name="offset">The node's offset within the graph.</param>
    public PositionedNodeData(BlockDef type, NodeSettingsCollection settings, short3 offset)
    {
        _type = type;
        _settings = settings._collection;
        _offset = offset;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PositionedNodeData"/> struct.
    /// </summary>
    /// <param name="type">Type of the node.</param>
    /// <param name="settings">Settings of the node.</param>
    /// <param name="offset">The node's offset within the graph.</param>
    public PositionedNodeData(BlockDef type, PrefabSettings settings, short3 offset)
    {
        _type = type;
        _offset = offset;

        _settings = [.. settings];
    }

    internal PositionedNodeData(BlockDef type, SettingsCollection settings, short3 offset)
    {
        _type = type;
        _settings = settings;
        _offset = offset;
    }

      /// <summary>
    /// Gets the type of the node.
    /// </summary>
    /// <value>Type of the node.</value>
    public BlockDef Type => _type;

    /// <summary>
    /// Gets the settings of the node.
    /// </summary>
    /// <value>Settings of the node.</value>
    public NodeSettingsCollection Settings => new NodeSettingsCollection(_settings);

    /// <summary>
    /// Gets the position offset of the node.
    /// </summary>
    /// <value>The node's offset within the graph.</value>
    public int3 Offset => _offset;
}