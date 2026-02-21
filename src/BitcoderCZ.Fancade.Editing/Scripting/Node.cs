// <copyright file="Node.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BitcoderCZ.Buffers;
using BitcoderCZ.Maths.Vectors;
using static BitcoderCZ.Utils.ThrowHelper;
using SettingsCollection = BitcoderCZ.Buffers.InlineList<BitcoderCZ.Buffers.FixedArray1<BitcoderCZ.Fancade.PrefabSetting>, BitcoderCZ.Fancade.PrefabSetting>;
using TerminalsOutBuffer = BitcoderCZ.Buffers.FixedArray1<BitcoderCZ.Fancade.Editing.Scripting.Node.Terminal>;
using TerminalsOutCollection = BitcoderCZ.Buffers.ImmutableInlineArray<BitcoderCZ.Buffers.FixedArray1<BitcoderCZ.Fancade.Editing.Scripting.Node.Terminal>, BitcoderCZ.Fancade.Editing.Scripting.Node.Terminal>;

namespace BitcoderCZ.Fancade.Editing.Scripting;

/// <summary>
/// Represents a node in a <see cref="CodeGraph"/>.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct Node : IEquatable<Node>
{
    /// <summary>
    /// Gets the empty node.
    /// </summary>
    /// <remarks>
    /// Can be used for padding.
    /// </remarks>
    /// <value>The empty node.</value>
    public static Node Empty { get; } = new Node(0, 0, new BlockDef("Empty", ushort.MaxValue, ScriptBlockType.NonScript, PrefabType.Normal, int3.One, TerminalBuilder.Empty));

    private readonly ushort _graphId;
    private readonly ushort _index; // todo: this could also be ushort
    private readonly BlockDef _type;

    internal Node(ushort graphId, ushort index, BlockDef type)
    {
        _graphId = graphId;
        _index = index;
        _type = type;
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
    /// Gets a value indicating whether the <see cref="Node"/> is empty/air.
    /// </summary>
    /// <value><see langword="true"/> if the <see cref="Node"/> is empty; otherwise, <see langword="false"/>.</value>
    public bool IsEmpty => Handle == NodeHandle.Null;

    /// <summary>Returns a value that indicates whether the 2 <see cref="Node"/>s are equal.</summary>
    /// <param name="left">The first <see cref="Node"/> to compare.</param>
    /// <param name="right">The second <see cref="Node"/> to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(Node left, Node right)
        => left.Equals(right);

    /// <summary>Returns a value that indicates whether the 2 <see cref="Node"/>s are not equal.</summary>
    /// <param name="left">The first <see cref="Node"/> to compare.</param>
    /// <param name="right">The second <see cref="Node"/> to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(Node left, Node right)
        => !(left == right);

    /// <inheritdoc/>
    public bool Equals(Node other)
        => Handle == other.Handle;

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj is Node other && Equals(other);

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

        internal BlockRegion(ushort graphId, ushort index, Array3D<ushort> blocks)
        {
            _graphId = graphId;
            _index = index;
            _blocks = blocks;
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
        public Array3D<ushort> Blocks => _blocks;

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

    /// <summary>
    /// Represents a connection between two <see cref="Terminal"/>s.
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    public readonly struct Connection : IEquatable<Connection>
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

        /// <summary>Returns a value that indicates whether the 2 <see cref="Connection"/>s are equal.</summary>
        /// <param name="left">The first <see cref="Connection"/> to compare.</param>
        /// <param name="right">The second <see cref="Connection"/> to compare.</param>
        /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are equal; otherwise, <see langword="false"/>.</returns>
        public static bool operator ==(Connection left, Connection right)
            => left.Equals(right);

        /// <summary>Returns a value that indicates whether the 2 <see cref="Connection"/>s are not equal.</summary>
        /// <param name="left">The first <see cref="Connection"/> to compare.</param>
        /// <param name="right">The second <see cref="Connection"/> to compare.</param>
        /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are not equal; otherwise, <see langword="false"/>.</returns>
        public static bool operator !=(Connection left, Connection right)
            => !(left == right);

        /// <summary>
        /// Returns a new <see cref="Connection"/> instance whose terminals are associated with the specified graph id.
        /// </summary>
        /// <param name="graphId">The graph id to assign to both the <c>From</c> and <c>To</c> terminals.</param>
        /// <returns>
        /// A new <see cref="Connection"/> instance with both terminals updated to use the specified graph id.
        /// </returns>
        public Connection WithGraphId(ushort graphId)
            => new Connection(From.WithGraphId(graphId), To.WithGraphId(graphId));

        /// <inheritdoc/>
        public bool Equals(Connection other)
            => From == other.From && To == other.To;

        /// <inheritdoc/>
        public override bool Equals(object? obj)
            => obj is Connection other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(From, To);
    }

    /// <summary>
    /// Type of a <see cref="Node.Terminal"/>.
    /// </summary>
    public enum TerminalType : byte
    {
        /// <summary>
        /// The terminal is on a <see cref="Node"/>.
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
        /// The terminal is an object connection to an offset inside of a <see cref="Node.BlockRegion"/>.
        /// </summary>
        ObjectRelative,
    }

    /// <summary>
    /// Represents a terminal, which can be connected to other terminals.
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    public readonly struct Terminal : IEquatable<Terminal>
    {
        /// <summary>
        /// Gets a null terminal.
        /// </summary>
        /// <value>A null terminal.</value>
        public static Terminal Null => default;

        internal readonly short3 _blockPositon;

        private const short TypeInVoxelPosition = short.MinValue;

        private readonly byte3 _voxelPosition;
        private readonly SignalType _signalType;
        private readonly ushort _graphId;
        private readonly ushort _nodeOrRegionIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="Terminal"/> struct.
        /// </summary>
        /// <param name="node">The node this terminal belongs to.</param>
        /// <param name="index">The index of the terminal (<see cref="TerminalDef.Index"/>).</param>
        public Terminal(Node node, int index)
        {
            ThrowIfNull(node);

            _graphId = node._graphId;
            _nodeOrRegionIndex = node._index;
            var def = node.Type.Terminals[index];
            _voxelPosition = def.Position;
            _signalType = def.SignalType;
            _blockPositon = new short3(TypeInVoxelPosition, (short)TerminalType.Node, (short)0);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Terminal"/> struct.
        /// </summary>
        /// <param name="node">The node this terminal belongs to.</param>
        /// <param name="name">The name of the terminal.</param>
        public Terminal(Node node, string name)
        {
            ThrowIfNull(node);
            ThrowIfNull(name);

            _graphId = node._graphId;
            _nodeOrRegionIndex = node._index;
            var def = node.Type[name];
            _voxelPosition = def.Position;
            _signalType = def.SignalType;
            _blockPositon = new short3(TypeInVoxelPosition, (short)TerminalType.Node, (short)0);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Terminal"/> struct.
        /// </summary>
        /// <param name="node">The node this terminal belongs to.</param>
        /// <param name="def">The terminal definition.</param>
        public Terminal(Node node, TerminalDef def)
        {
            ThrowIfNull(node);

            // todo: verify that def is on node.Type?
            _graphId = node._graphId;
            _nodeOrRegionIndex = node._index;
            _voxelPosition = def.Position;
            _signalType = def.SignalType;
            _blockPositon = new short3(TypeInVoxelPosition, (short)TerminalType.Node, (short)0);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Terminal"/> struct.
        /// </summary>
        /// <param name="node">The node this terminal belongs to.</param>
        /// <param name="def">The terminal definition.</param>
        public Terminal(NodeHandle node, TerminalDef def)
        {
            ThrowIfNull(node);

            // todo: verify that def is on node.Type?
            _graphId = node._graphId;
            _nodeOrRegionIndex = node._index;
            _voxelPosition = def.Position;
            _signalType = def.SignalType;
            _blockPositon = new short3(TypeInVoxelPosition, (short)TerminalType.Node, (short)0);
        }

        private Terminal(short3 blockPosition, byte3 voxelPositon, SignalType signalType)
        {
            _blockPositon = blockPosition;
            _voxelPosition = voxelPositon;
            _signalType = signalType;
        }

        private Terminal(BlockRegionHandle region, short3 positionInRange, byte3 voxelPositon, SignalType signalType)
        {
            _graphId = region._graphId;
            _nodeOrRegionIndex = region._index;
            _blockPositon = positionInRange;
            _voxelPosition = voxelPositon;
            _signalType = signalType;
        }

        private Terminal(short3 blockPositon, byte3 voxelPosition, SignalType signalType, ushort graphId, ushort nodeOrRegionIndex)
        {
            _blockPositon = blockPositon;
            _voxelPosition = voxelPosition;
            _signalType = signalType;
            _graphId = graphId;
            _nodeOrRegionIndex = nodeOrRegionIndex;
        }

        /// <summary>
        /// Gets a value indicating whether this terminal is <see langword="null"/>.
        /// </summary>
        /// <value><see langword="true"/> if the terminal is <see langword="null"/>; otherwise, <see langword="false"/>.</value>
        public readonly bool IsNull => this == default;

        /// <summary>
        /// Gets the type of the terminal.
        /// </summary>
        /// <value>Type of the terminal.</value>
        public readonly TerminalType Type
        {
            get
            {
                if (_blockPositon.X is TypeInVoxelPosition)
                {
                    Debug.Assert(((TerminalType)_blockPositon.Y) is TerminalType.Node or TerminalType.OutsideInput or TerminalType.OutsideOutput);
                    return (TerminalType)_blockPositon.Y;
                }

                if (_graphId is 0)
                {
                    return TerminalType.ObjectAbsolute;
                }

                return TerminalType.ObjectRelative;
            }
        }

        /// <summary>
        /// Gets the node this terminal belongs to.
        /// </summary>
        /// <value>The node this terminal belongs to; or <see langword="null"/>, if <see cref="Type"/> is not <see cref="TerminalType.Node"/>.</value>
        public readonly NodeHandle? Node => Type is TerminalType.Node ? new NodeHandle(_graphId, _nodeOrRegionIndex) : null;

        /// <summary>
        /// Gets the <see cref="BlockRegion"/> this terminal belongs to.
        /// </summary>
        /// <value>The <see cref="BlockRegion"/> this terminal belongs to; or <see langword="null"/>, if <see cref="Type"/> is not <see cref="TerminalType.ObjectRelative"/>.</value>
        public readonly BlockRegionHandle? Region => Type is TerminalType.ObjectRelative ? new BlockRegionHandle(_graphId, _nodeOrRegionIndex) : null;

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
        public readonly byte3 VoxelPosition => _voxelPosition;

        /// <summary>
        /// Gets the <see cref="BitcoderCZ.Fancade.SignalType"/> of the terminal.
        /// </summary>
        /// <value>The <see cref="BitcoderCZ.Fancade.SignalType"/> of the terminal.</value>
        public readonly SignalType SignalType => _signalType;

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

        /// <summary>
        /// Creates an external input terminal.
        /// </summary>
        /// <param name="voxelPositon">Voxel position on the outside prefab.</param>
        /// <param name="signalType">Type of the terminal.</param>
        /// <returns>The contructed terminal.</returns>
        public static Terminal OutsideInput(byte3 voxelPositon, SignalType signalType)
            => new Terminal(new short3(TypeInVoxelPosition, (short)TerminalType.OutsideInput, (short)0), voxelPositon, signalType);

        /// <summary>
        /// Creates an external output terminal.
        /// </summary>
        /// <param name="voxelPositon">Voxel position on the outside prefab.</param>
        /// <param name="signalType">Type of the terminal.</param>
        /// <returns>The contructed terminal.</returns>
        public static Terminal OutsideOutput(byte3 voxelPositon, SignalType signalType)
            => new Terminal(new short3(TypeInVoxelPosition, (short)TerminalType.OutsideOutput, (short)0), voxelPositon, signalType);

        /// <summary>
        /// Creates an object terminal at a given block position, used to connect to non script blocks.
        /// </summary>
        /// <param name="blockPosition">Absolute position of the block to connect to.</param>
        /// <param name="voxelPositon">Voxel position on the outside prefab.</param>
        /// <returns>The contructed terminal.</returns>
        public static Terminal ObjectAbsolute(int3 blockPosition, byte3 voxelPositon)
            => new Terminal(checked((short3)blockPosition), voxelPositon, SignalType.Obj);

        /// <summary>
        /// Creates an object terminal at a given block position, used to connect to non script blocks.
        /// </summary>
        /// <param name="region">The <see cref="BlockRegion"/> to which <paramref name="positionInRegion"/> is relative to.</param>
        /// <param name="positionInRegion">Position relative to <paramref name="region"/>.</param>
        /// <param name="voxelPositon">Voxel position on the outside prefab.</param>
        /// <returns>The contructed terminal.</returns>
        public static Terminal ObjectRelative(BlockRegion region, int3 positionInRegion, byte3 voxelPositon)
            => ObjectRelative(region.Handle, positionInRegion, voxelPositon);

        /// <summary>
        /// Creates an object terminal at a given block position, used to connect to non script blocks.
        /// </summary>
        /// <param name="region">The <see cref="BlockRegionHandle"/> to which <paramref name="positionInRegion"/> is relative to.</param>
        /// <param name="positionInRegion">Position relative to <paramref name="region"/>.</param>
        /// <param name="voxelPositon">Voxel position on the outside prefab.</param>
        /// <returns>The contructed terminal.</returns>
        public static Terminal ObjectRelative(BlockRegionHandle region, int3 positionInRegion, byte3 voxelPositon)
            => new Terminal(region, checked((short3)positionInRegion), voxelPositon, SignalType.Obj);

        /// <summary>
        /// Returns a <see cref="Terminal"/> instance with the specified graph identifier, if the terminal is a node or block region terminal.
        /// </summary>
        /// <param name="graphId">The graph identifier to associate with the terminal.</param>
        /// <returns>
        /// The current <see cref="Terminal"/> instance if no graph is assigned; otherwise, a new <see cref="Terminal"/> instance with the specified graph id.
        /// </returns>
        public Terminal WithGraphId(ushort graphId)
            => _graphId is 0 ? this : new Terminal(_blockPositon, _voxelPosition, _signalType, graphId, _nodeOrRegionIndex);

        /// <inheritdoc/>
        public bool Equals(Terminal other)
            => _graphId == other._graphId && _nodeOrRegionIndex == other._nodeOrRegionIndex && _blockPositon == other._blockPositon && _voxelPosition == other._voxelPosition && _signalType == other._signalType;

        /// <inheritdoc/>
        public override bool Equals(object? obj)
            => obj is Terminal terminal && Equals(terminal);

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(_graphId, _nodeOrRegionIndex, _blockPositon, _voxelPosition, _signalType);

        /// <summary>
        /// Creates an object terminal at a given block position.
        /// </summary>
        /// <param name="region">The <see cref="BlockRegionHandle"/> to which <paramref name="positionInRange"/> is relative to.</param>
        /// <param name="positionInRange">Position relative to <paramref name="region"/>.</param>
        /// <param name="voxelPositon">Voxel position on the outside prefab.</param>
        /// <param name="signalType">Type of the terminal.</param>
        /// <returns>The contructed terminal.</returns>
        internal static Terminal ObjectRelative(BlockRegionHandle region, int3 positionInRange, byte3 voxelPositon, SignalType signalType)
            => new Terminal(region, checked((short3)positionInRange), voxelPositon, signalType);

        internal Terminal WithGraphId(ushort graphId, int indexOffset)
            => _graphId is 0 ? this : new Terminal(_blockPositon, _voxelPosition, _signalType, graphId, (ushort)(indexOffset + _nodeOrRegionIndex));
    }

    /// <summary>
    /// Represents a lightweight container that groups an input terminal with one or more output terminals.
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    public readonly struct TerminalStore
    {
        private readonly TerminalsOutCollection _outTerminals;

        /// <summary>
        /// Initializes a new instance of the <see cref="TerminalStore"/> struct, with an input terminal and one or more output terminals.
        /// </summary>
        /// <param name="in">The input terminal.</param>
        /// <param name="out">The output terminals.</param>
        public TerminalStore(Terminal @in, params ReadOnlySpan<Terminal> @out)
        {
            In = @in;
            _outTerminals = ImmutableInlineArray.Create<TerminalsOutBuffer, Terminal>(@out);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TerminalStore"/> struct, with an input terminal and a sequence of output terminals.
        /// </summary>
        /// <param name="in">The input terminal.</param>
        /// <param name="out">The output terminals.</param>
        public TerminalStore(Terminal @in, IEnumerable<Terminal> @out)
        {
            In = @in;
            _outTerminals = ImmutableInlineArray.CreateRange<TerminalsOutBuffer, Terminal>(@out);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TerminalStore"/> struct, using an existing output terminal collection.
        /// </summary>
        /// <param name="in">The input terminal.</param>
        /// <param name="out">An enumerable view of the output terminals.</param>
        public TerminalStore(Terminal @in, OutTerminalsCollection @out)
        {
            In = @in;
            _outTerminals = @out._outTerminals;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TerminalStore"/> struct from an active script node.
        /// </summary>
        /// <param name="node">The <see cref="Node"/> to create this <see cref="TerminalStore"/> from, must be <see cref="ScriptBlockType.Active"/>.</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="node"/> is not an active script node.</exception>
        public TerminalStore(Node node)
            : this(new Terminal(node, node.Type.Before), new Terminal(node, node.Type.After))
        {
            if (node.Type.BlockType != ScriptBlockType.Active)
            {
                ThrowArgumentException($"{nameof(node)}.{nameof(Block.Type)}.{nameof(BlockDef.BlockType)} must be {nameof(ScriptBlockType)}.{nameof(ScriptBlockType.Active)}.", nameof(node));
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TerminalStore"/> struct from an active script node.
        /// </summary>
        /// <param name="node">The <see cref="NodeHandle"/> to create this <see cref="TerminalStore"/> from.</param>
        /// <param name="type">Type of the node, must be <see cref="ScriptBlockType.Active"/>.</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="node"/> is not an active script node.</exception>
        public TerminalStore(NodeHandle node, BlockDef type)
            : this(new Terminal(node, type.Before), new Terminal(node, type.After))
        {
            if (type.BlockType != ScriptBlockType.Active)
            {
                ThrowArgumentException($"{nameof(node)}.{nameof(Block.Type)}.{nameof(BlockDef.BlockType)} must be {nameof(ScriptBlockType)}.{nameof(ScriptBlockType.Active)}.", nameof(node));
            }
        }

        /// <summary>
        /// Gets the input terminal.
        /// </summary>
        /// <value>The input terminal.</value>
        public Terminal In { get; }

        /// <summary>
        /// Gets the output terminals.
        /// </summary>
        /// <value>An enumerable collection of output terminals.</value>
        public OutTerminalsCollection Out => new OutTerminalsCollection(_outTerminals);

        /// <summary>
        /// Gets the number of output terminals.
        /// </summary>
        /// <value>The number of output terminals.</value>
        public int OutCount => _outTerminals.Length;

        /// <summary>
        /// Creates a <see cref="TerminalStore"/> containing only an input terminal.
        /// </summary>
        /// <param name="in">The input terminal.</param>
        /// <returns>A <see cref="TerminalStore"/> with no output terminals.</returns>
        public static TerminalStore CreateIn(Terminal @in)
            => new TerminalStore(@in);

        /// <summary>
        /// Creates a <see cref="TerminalStore"/> containing only output terminals.
        /// </summary>
        /// <param name="out">The output terminals.</param>
        /// <returns>A <see cref="TerminalStore"/> with no input terminal.</returns>
        public static TerminalStore CreateOut(params ReadOnlySpan<Terminal> @out)
            => new TerminalStore(default, @out);

        /// <summary>
        /// Combines the input terminal from one <see cref="TerminalStore"/> with the output terminals of another.
        /// </summary>
        /// <param name="in">The <see cref="TerminalStore"/> providing the input terminal.</param>
        /// <param name="out">The <see cref="TerminalStore"/> providing the output terminals.</param>
        /// <returns>
        /// A new <see cref="TerminalStore"/> containing the input terminal from <paramref name="in"/> and the output terminals from <paramref name="out"/>.
        /// </returns>
        public static TerminalStore Combine(TerminalStore @in, TerminalStore @out)
            => new TerminalStore(@in.In, @out.Out);

        /// <summary>
        /// A collection of terminals.
        /// </summary>
        [StructLayout(LayoutKind.Auto)]
        public readonly struct OutTerminalsCollection : IReadOnlyList<Terminal>
        {
            internal readonly TerminalsOutCollection _outTerminals;

            internal OutTerminalsCollection(TerminalsOutCollection outTerminals)
            {
                _outTerminals = outTerminals;
            }

            /// <inheritdoc/>
            public Terminal this[int index] => _outTerminals[index];

            /// <inheritdoc/>
            public int Count => _outTerminals.Length;

            /// <inheritdoc cref="IEnumerable.GetEnumerator"/>
            public OutTerminalsEnumerator GetEnumerator()
                => new OutTerminalsEnumerator(_outTerminals.GetEnumerator());

            /// <inheritdoc/>
            IEnumerator<Terminal> IEnumerable<Terminal>.GetEnumerator()
                => GetEnumerator();

            /// <inheritdoc/>
            IEnumerator IEnumerable.GetEnumerator()
                => GetEnumerator();
        }

        /// <summary>
        /// Enumerates a collection of output terminals.
        /// </summary>
        [StructLayout(LayoutKind.Auto)]
        public struct OutTerminalsEnumerator : IEnumerator<Terminal>
        {
            private TerminalsOutCollection.Enumerator _enumerator;

            internal OutTerminalsEnumerator(TerminalsOutCollection.Enumerator enumerator)
            {
                _enumerator = enumerator;
            }

            /// <inheritdoc/>
            public readonly Terminal Current => _enumerator.Current;

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
}

/// <summary>
/// Uniquely identifies a node.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct NodeHandle : IEquatable<NodeHandle>
{
    /// <summary>
    /// Gets the null handle, represents an empty block of size 1x1x1, can be used for spacing.
    /// </summary>
    /// <value>Null handle.</value>
    public static NodeHandle Null => default;

    internal readonly ushort _graphId;
    internal readonly ushort _index;

    internal NodeHandle(ushort graphId, ushort index)
    {
        _graphId = graphId;
        _index = index;
    }

    /// <summary>
    /// Gets the id of the graph the node belongs to.
    /// </summary>
    /// <value>Id of the graph the node belongs to.</value>
    public int GraphId => _graphId;

    /// <summary>
    /// Gets the index of the node in <see cref="CodeGraph"/>.
    /// </summary>
    /// <value>Index of the node in <see cref="CodeGraph"/>.</value>
    public int Index => _index;

    /// <summary>Returns a value that indicates whether the 2 <see cref="NodeHandle"/>s are equal.</summary>
    /// <param name="left">The first <see cref="NodeHandle"/> to compare.</param>
    /// <param name="right">The second <see cref="NodeHandle"/> to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(NodeHandle left, NodeHandle right)
        => left.Equals(right);

    /// <summary>Returns a value that indicates whether the 2 <see cref="NodeHandle"/>s are not equal.</summary>
    /// <param name="left">The first <see cref="NodeHandle"/> to compare.</param>
    /// <param name="right">The second <see cref="NodeHandle"/> to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(NodeHandle left, NodeHandle right)
        => !(left == right);

    /// <inheritdoc/>
    public bool Equals(NodeHandle other)
        => _graphId == other._graphId && _index == other._index;

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj is NodeHandle other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
        => HashCode.Combine(_graphId, _index);
}

/// <summary>
/// Uniquely identifies a block region.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct BlockRegionHandle : IEquatable<BlockRegionHandle>
{
    internal readonly ushort _graphId;
    internal readonly ushort _index;

    internal BlockRegionHandle(ushort graphId, ushort index)
    {
        _graphId = graphId;
        _index = index;
    }

    /// <summary>
    /// Gets the id of the graph the region belongs to.
    /// </summary>
    /// <value>Id of the graph the region belongs to.</value>
    public int GraphId => _graphId;

    /// <summary>
    /// Gets the index of the region in <see cref="CodeGraph"/>.
    /// </summary>
    /// <value>Index of the region in <see cref="CodeGraph"/>.</value>
    public int Index => _index;

    /// <summary>Returns a value that indicates whether the 2 <see cref="BlockRegionHandle"/>s are equal.</summary>
    /// <param name="left">The first <see cref="BlockRegionHandle"/> to compare.</param>
    /// <param name="right">The second <see cref="BlockRegionHandle"/> to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(BlockRegionHandle left, BlockRegionHandle right)
        => left.Equals(right);

    /// <summary>Returns a value that indicates whether the 2 <see cref="BlockRegionHandle"/>s are not equal.</summary>
    /// <param name="left">The first <see cref="BlockRegionHandle"/> to compare.</param>
    /// <param name="right">The second <see cref="BlockRegionHandle"/> to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(BlockRegionHandle left, BlockRegionHandle right)
        => !(left == right);

    /// <inheritdoc/>
    public bool Equals(BlockRegionHandle other)
        => _graphId == other._graphId && _index == other._index;

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj is BlockRegionHandle other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
        => HashCode.Combine(_graphId, _index);
}

/// <summary>
/// Collection of <see cref="PrefabSetting"/>s.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct NodeSettingsCollection : IReadOnlyCollection<PrefabSetting>
{
    internal readonly SettingsCollection _collection;

    internal NodeSettingsCollection(SettingsCollection collection)
    {
        _collection = collection;
    }

    /// <inheritdoc/>
    public int Count => _collection.Count;

    /// <inheritdoc cref="IEnumerable.GetEnumerator"/>
    public Enumerator GetEnumerator()
        => new Enumerator(_collection.GetEnumerator());

    /// <inheritdoc/>
    IEnumerator<PrefabSetting> IEnumerable<PrefabSetting>.GetEnumerator()
        => GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    /// <summary>
    /// Enumerates a collection of <see cref="PrefabSetting"/>s.
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    public struct Enumerator : IEnumerator<PrefabSetting>
    {
        private SettingsCollection.Enumerator _enumerator;

        internal Enumerator(SettingsCollection.Enumerator enumerator)
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

[StructLayout(LayoutKind.Auto)]
internal struct NodeData
{
    public BlockDef? Type;
    public SettingsCollection Settings;

    public NodeData(BlockDef? type, SettingsCollection settings)
    {
        Type = type;
        Settings = settings;
    }
}