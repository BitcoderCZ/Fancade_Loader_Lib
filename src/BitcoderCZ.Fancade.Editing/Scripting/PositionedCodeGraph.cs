// <copyright file="PositionedCodeGraph.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BitcoderCZ.Fancade.Editing.Scripting.Utils;
using BitcoderCZ.Maths.Vectors;
using static BitcoderCZ.Utils.ThrowHelper;
using SettingsCollection = BitcoderCZ.Buffers.InlineList<BitcoderCZ.Buffers.FixedArray2<BitcoderCZ.Fancade.PrefabSetting>, BitcoderCZ.Fancade.PrefabSetting>;

namespace BitcoderCZ.Fancade.Editing.Scripting;

/// <summary>
/// Represents a positioned graph of code nodes and connections.
/// </summary>
public sealed class PositionedCodeGraph
{
    private readonly PositionedNode[] _nodes;
    private readonly PositionedNode.BlockRegion[] _regions;
    private readonly PositionedNode.Connection[] _connections;

    internal PositionedCodeGraph(PositionedNode[] nodes, ReadOnlySpan<Node.BlockRegion> regions, ReadOnlySpan<Node.Connection> connections, int3 size)
    {
        _nodes = nodes;
        Size = size;

        var positions = CodeGraphEmitHelper.Pack(size, regions);

        Offset = positions[0];

        var positionedRegions = new PositionedNode.BlockRegion[regions.Length];

        foreach (var region in regions)
        {
            positionedRegions[region._index] = new PositionedNode.BlockRegion(region.Blocks, positions[region._index+1], region._index);
        }

        _regions = positionedRegions;

        var positionedConnections = new PositionedNode.Connection[connections.Length];

        int index = 0;
        foreach (var connection in connections)
        {
            positionedConnections[index++] = PositionedNode.Connection.Cast(connection, region => positionedRegions[region._index]);
        }

        _connections = positionedConnections;
    }

    /// <summary>
    /// Gets the size of the graph.
    /// </summary>
    /// <value>Size of the region containing all nodes, does not include <see cref="Offset"/> or <see cref="Regions"/>.</value>
    public int3 Size { get; }

    /// <summary>
    /// Gets the offset of the nodes in the graph.
    /// </summary>
    /// <value>Offset of the nodes in the graph.</value>
    public int3 Offset { get; }

    /// <summary>
    /// Gets the positioned nodes contained in the graph.
    /// </summary>
    /// <value>A read-only list of positioned nodes.</value>
    public IReadOnlyList<PositionedNode> Nodes => _nodes;

    /// <summary>
    /// Gets the positioned nodes contained in the graph as a span.
    /// </summary>
    /// <value>A read-only span of positioned nodes.</value>
    public ReadOnlySpan<PositionedNode> NodesSpan => _nodes;

    /// <summary>
    /// Gets the <see cref="Node.BlockRegion"/>s in the <see cref="PositionedCodeGraph"/>.
    /// </summary>
    /// <value>The <see cref="Node.BlockRegion"/>s in the <see cref="PositionedCodeGraph"/>.</value>
    public IReadOnlyList<PositionedNode.BlockRegion> Regions => _regions;

    /// <summary>
    /// Gets the <see cref="Node.BlockRegion"/>s in the <see cref="PositionedCodeGraph"/>.
    /// </summary>
    /// <value>The <see cref="Node.BlockRegion"/>s in the <see cref="PositionedCodeGraph"/>.</value>
    public ReadOnlySpan<PositionedNode.BlockRegion> RegionsSpan => _regions;

    /// <summary>
    /// Gets the connections between nodes in the graph.
    /// </summary>
    /// <value>A read-only list of node connections.</value>
    public IReadOnlyList<PositionedNode.Connection> Connections => _connections;

    /// <summary>
    /// Gets the connections between nodes in the graph as a span.
    /// </summary>
    /// <value>A read-only span of node connections.</value>
    public ReadOnlySpan<PositionedNode.Connection> ConnectionSpan => _connections;
}

/// <summary>
/// Represents a node in a <see cref="PositionedCodeGraph"/>.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct PositionedNode
{
    internal readonly SettingsCollection _settings;
    internal readonly int _index;

    internal PositionedNode(int3 offset, BlockDef type, SettingsCollection settings, int index)
    {
        Debug.Assert(type is not null);
        
        Offset = offset;
        Type = type;
        _settings = settings;
        _index = index;
    }

    /// <summary>
    /// Gets the index of the node in <see cref="PositionedCodeGraph"/>.
    /// </summary>
    /// <value>Index of the node in <see cref="PositionedCodeGraph"/>.</value>
    public int Index => _index;

    /// <summary>
    /// Gets the position offset of the node.
    /// </summary>
    /// <value>The node's offset within the graph.</value>
    public int3 Offset { get; }

    /// <summary>
    /// Gets the type of block this node represents.
    /// </summary>
    /// <value>Type of block this node represents.</value>
    public BlockDef Type { get; }

    /// <summary>
    /// Gets the settings applied to this node.
    /// </summary>
    /// <value>An enumerable that iterates the settings applied to this node.</value>
    public SettingsEnumerable Settings => new SettingsEnumerable(this);

    /// <summary>
    /// Gets a value indicating whether the <see cref="Node"/> is empty/air.
    /// </summary>
    /// <value><see langword="true"/> if the <see cref="Node"/> is empty; otherwise, <see langword="false"/>.</value>
    public bool IsEmpty => Type is null || Type.Prefab.Id is 0;

    /// <summary>
    /// Represents a region of non-script blocks that can be constructed and later connected to script nodes.
    /// </summary>
    public sealed class BlockRegion
    {
        internal int _index;

        internal BlockRegion(IReadOnly3DArray<ushort> blocks, int3 offset, int index)
        {
            Blocks = blocks;
            Offset = offset;
            _index = index;
        }

        /// <summary>
        /// Gets the index of the region in <see cref="PositionedCodeGraph"/>.
        /// </summary>
        /// <value>Index of the region in <see cref="PositionedCodeGraph"/>.</value>
        public int Index => _index;

        /// <summary>
        /// Gets the blocks contained in this region.
        /// </summary>
        /// <value>A 3D array indexed using region-relative block coordinates.</value>
        public IReadOnly3DArray<ushort> Blocks { get; }

        /// <summary>
        /// Gets the position offset of the range.
        /// </summary>
        /// <value>The range's offset within the graph.</value>
        public int3 Offset { get; }
    }

    /// <summary>
    /// Represents a connection between two <see cref="Terminal"/>s.
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    public readonly struct Connection
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Connection"/> struct.
        /// </summary>
        /// <param name="from">The source <see cref="Terminal"/>.</param>
        /// <param name="to">The target <see cref="Terminal"/>.</param>
        public Connection(Terminal from, Terminal to)
        {
            From = from;
            To = to;
        }

        /// <summary>
        /// Gets the source terminal of the connection.
        /// </summary>
        /// <value>Source terminal of the connection.</value>
        public readonly Terminal From { get; }

        /// <summary>
        /// Gets the target terminal of the connection.
        /// </summary>
        /// <value>Target terminal of the connection.</value>
        public readonly Terminal To { get; }

        internal static Connection Cast(Node.Connection connection, Func<Node.BlockRegion, PositionedNode.BlockRegion> mapRegion)
            => new Connection(Terminal.Cast(connection.From, mapRegion), Terminal.Cast(connection.To, mapRegion));
    }

    /// <summary>
    /// Type of a <see cref="Terminal"/>.
    /// </summary>
    public enum TerminalType
    {
        /// <summary>
        /// The terminal is on a <see cref="PositionedNode"/>.
        /// </summary>
        Node,

        /// <summary>
        /// The terminal is an input from the outside.
        /// </summary>
        OutsideInput,

        /// <summary>
        /// The terminal is an output to the outside.
        /// </summary>
        OutsideOutput,

        /// <summary>
        /// The terminal is an object connection to an absolute position.
        /// </summary>
        ObjectAbsolute,

        /// <summary>
        /// The terminal is an object connection to an offset inside of a <see cref="BlockRegion"/>.
        /// </summary>
        ObjectRelative,
    }

    /// <summary>
    /// Represents a terminal, which can be connected to other terminals.
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    public readonly struct Terminal : IEquatable<Terminal>
    {
        private const short BlockPosInput = 0;
        private const short BlockPosOutput = 1;
        private const short BlockPosNode = 2;

        private static readonly short FromToOutsideValue = unchecked((short)Fancade.Connection.IsFromToOutsideValue);

        private readonly BlockRegion? _region;
        private readonly byte _index;
        private readonly short3 _blockPositon;

        private Terminal(BlockRegion? region, short3 blockPositon, byte3 voxelPositon, int index, SignalType signalType)
        {
            _region = region;
            _blockPositon = blockPositon;
            _index = (byte)index;
            VoxelPosition = voxelPositon;
            SignalType = signalType;
        }

        /// <summary>
        /// Gets the type of the terminal.
        /// </summary>
        /// <value>Type of the terminal.</value>
        public readonly TerminalType Type
        {
            get
            {
                if (_region is { })
                {
                    return TerminalType.ObjectRelative;
                }
                else if (_blockPositon.X == FromToOutsideValue)
                {
                    return _blockPositon.Y switch
                    {
                        BlockPosInput => TerminalType.OutsideInput,
                        BlockPosOutput => TerminalType.OutsideOutput,
                        BlockPosNode => TerminalType.Node,
                        _ => default,
                    };
                }
                else
                {
                    return TerminalType.ObjectAbsolute;
                }
            }
        }

        /// <summary>
        /// Gets the index of the node this terminal belongs to.
        /// </summary>
        /// <value>Index of the node this terminal belongs to; or <see langword="null"/>, if <see cref="Type"/> is not <see cref="TerminalType.Node"/>.</value>
        public readonly int? NodeIndex => _blockPositon.Y is BlockPosNode ? _blockPositon.Z : null;

        /// <summary>
        /// Gets the <see cref="BlockRegion"/> this terminal belongs to.
        /// </summary>
        /// <value>The <see cref="BlockRegion"/> this terminal belongs to; or <see langword="null"/>, if <see cref="Type"/> is not <see cref="TerminalType.ObjectRelative"/>.</value>
        public readonly BlockRegion? Region => _region;

        /// <summary>
        /// Gets the block position.
        /// </summary>
        /// <value>
        /// If <see cref="Type"/> is <see cref="TerminalType.ObjectAbsolute"/>, the absolute block positon of the object;
        /// if <see cref="TerminalType.ObjectRelative"/>, the offset into <see cref="Region"/>;
        /// otherwise, <see langword="null"/>.
        /// </value>
        public readonly int3? BlockPostion => Type is TerminalType.Node or TerminalType.OutsideInput or TerminalType.OutsideOutput ? null : _blockPositon;

        /// <summary>
        /// Gets the voxel position of this terminal.
        /// </summary>
        /// <value>Voxel position of this terminal.</value>
        public readonly byte3 VoxelPosition { get; }

        /// <summary>
        /// Gets the index of this terminal (<see cref="TerminalDef.Index"/>).
        /// </summary>
        /// <value>Index of this terminal (<see cref="TerminalDef.Index"/>).</value>
        public readonly int Index => _index;

        /// <summary>
        /// Gets the <see cref="BitcoderCZ.Fancade.SignalType"/> of the terminal.
        /// </summary>
        /// <value>The <see cref="BitcoderCZ.Fancade.SignalType"/> of the terminal.</value>
        public readonly SignalType SignalType { get; }

        /// <summary>Returns a value that indicates whether the 2 <see cref="Terminal"/>s are equal.</summary>
        /// <param name="left">The first <see cref="Terminal"/> to compare.</param>
        /// <param name="right">The second <see cref="Terminal"/> to compare.</param>
        /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are equal; otherwise, <see langword="false"/>.</returns>
        public static bool operator ==(Terminal left, Terminal right)
            => left.Equals(right);

        /// <summary>Returns a value that indicates whether the 2 <see cref="Terminal"/>s are not equal.</summary>
        /// <param name="left">The first <see cref="Terminal"/> to compare.</param>
        /// <param name="right">The second <see cref="Terminal"/> to compare.</param>
        /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are not equal; otherwise, <see langword="false"/>.</returns>
        public static bool operator !=(Terminal left, Terminal right)
            => !(left == right);

        /// <inheritdoc/>
        public bool Equals(Terminal other)
            => Index == other.Index && _region == other._region && _blockPositon == other._blockPositon && VoxelPosition == other.VoxelPosition && SignalType == other.SignalType;

        /// <inheritdoc/>
        public override bool Equals(object? obj)
            => obj is Terminal terminal && Equals(terminal);

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(RuntimeHelpers.GetHashCode(_region), _blockPositon, _index, VoxelPosition, SignalType);

        internal static Terminal Cast(Node.Terminal terminal, Func<Node.BlockRegion, PositionedNode.BlockRegion> mapRegion)
            => terminal.Type switch
            {
                Node.TerminalType.Node => new Terminal(null, new(FromToOutsideValue, BlockPosNode, (short)terminal.Node!._index), terminal.VoxelPosition, terminal.Index, terminal.SignalType),
                Node.TerminalType.OutsideInput => new Terminal(null, new(FromToOutsideValue, BlockPosInput, (short)0), terminal.VoxelPosition, terminal.Index, terminal.SignalType),
                Node.TerminalType.OutsideOutput => new Terminal(null, new(FromToOutsideValue, BlockPosOutput, (short)0), terminal.VoxelPosition, terminal.Index, terminal.SignalType),
                Node.TerminalType.ObjectAbsolute => new Terminal(null, terminal._blockPositon, terminal.VoxelPosition, terminal.Index, terminal.SignalType),
                Node.TerminalType.ObjectRelative => new Terminal(mapRegion(terminal.Region!), terminal._blockPositon, terminal.VoxelPosition, terminal.Index, terminal.SignalType),
                _ => default,
            };
    }

    /// <summary>
    /// Provides an allocation-free enumerable over a collection of <see cref="PrefabSetting"/>s.
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    public readonly struct SettingsEnumerable : IEnumerable<PrefabSetting>
    {
        private readonly PositionedNode _node;

        internal SettingsEnumerable(PositionedNode node)
        {
            _node = node;
        }

        /// <inheritdoc cref="IEnumerable.GetEnumerator"/>
        public SettingsEnumerator GetEnumerator()
            => new SettingsEnumerator(_node._settings.GetEnumerator());

        /// <inheritdoc/>
        IEnumerator<PrefabSetting> IEnumerable<PrefabSetting>.GetEnumerator()
            => GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }

    /// <summary>
    /// Enumerates a collection of <see cref="PrefabSetting"/>s.
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    public struct SettingsEnumerator : IEnumerator<PrefabSetting>
    {
        private SettingsCollection.Enumerator _enumerator;

        internal SettingsEnumerator(SettingsCollection.Enumerator enumerator)
        {
            _enumerator = enumerator;
        }

        /// <inheritdoc/>
        public readonly PrefabSetting Current => _enumerator.Current;

        /// <inheritdoc/>
        readonly object IEnumerator.Current => Current;

        /// <inheritdoc/>
        public bool MoveNext() => _enumerator.MoveNext();

#pragma warning disable IDE0251 // Make member 'readonly'
        /// <inheritdoc/>
        void IEnumerator.Reset() => ((IEnumerator)_enumerator).Reset();
#pragma warning restore IDE0251 // Make member 'readonly'

        /// <inheritdoc/>
        readonly void IDisposable.Dispose()
        {
        }
    }
}