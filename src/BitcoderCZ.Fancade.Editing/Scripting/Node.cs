// <copyright file="Node.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BitcoderCZ.Buffers;
using BitcoderCZ.Maths.Vectors;
using static BitcoderCZ.Utils.ThrowHelper;
using SettingsCollection = BitcoderCZ.Buffers.InlineList<BitcoderCZ.Buffers.FixedArray2<BitcoderCZ.Fancade.PrefabSetting>, BitcoderCZ.Fancade.PrefabSetting>;
using TerminalsOutBuffer = BitcoderCZ.Buffers.FixedArray2<BitcoderCZ.Fancade.Editing.Scripting.Node.Terminal>;
using TerminalsOutCollection = BitcoderCZ.Buffers.ImmutableInlineArray<BitcoderCZ.Buffers.FixedArray2<BitcoderCZ.Fancade.Editing.Scripting.Node.Terminal>, BitcoderCZ.Fancade.Editing.Scripting.Node.Terminal>;

namespace BitcoderCZ.Fancade.Editing.Scripting;

/// <summary>
/// Represents a node in a <see cref="CodeGraph"/>.
/// </summary>
public sealed class Node
{
    internal readonly int _index;
    internal SettingsCollection _settings;

    internal Node(BlockDef type, int index)
    {
        Type = type;
        _index = index;
    }

    /// <summary>
    /// Gets the index of the node in <see cref="CodeGraph"/>.
    /// </summary>
    /// <value>Index of the node in <see cref="CodeGraph"/>.</value>
    public int Index => _index;

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

    /// <inheritdoc/>
    public override int GetHashCode()
        => _index;

    /// <summary>
    /// Represents a region of non-script blocks that can be constructed and later connected to script nodes.
    /// </summary>
    public sealed class BlockRegion
    {
        internal readonly int _index;

        internal BlockRegion(Array3D<ushort> blocks, int index)
        {
            Blocks = blocks;
            _index = index;
        }

        /// <summary>
        /// Gets the index of the region in <see cref="CodeGraph"/>.
        /// </summary>
        /// <value>Index of the region in <see cref="CodeGraph"/>.</value>
        public int Index => _index;

        /// <summary>
        /// Gets the blocks contained in this region.
        /// </summary>
        /// <value>A 3D array indexed using region-relative block coordinates.</value>
        public Array3D<ushort> Blocks { get; }
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
    }

    /// <summary>
    /// Type of a <see cref="Terminal"/>.
    /// </summary>
    public enum TerminalType
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
        /// <summary>
        /// Gets a null terminal.
        /// </summary>
        /// <value>A null terminal.</value>
        public static Terminal Null => default;

        internal readonly short3 _blockPositon;

        private static readonly short FromToOutsideValue = unchecked((short)Fancade.Connection.IsFromToOutsideValue);

        private readonly object? _nodeOrRegion;
        private readonly byte _index;

        /// <summary>
        /// Initializes a new instance of the <see cref="Terminal"/> struct.
        /// </summary>
        /// <param name="node">The node this terminal belongs to.</param>
        /// <param name="index">The index of the terminal (<see cref="TerminalDef.Index"/>).</param>
        public Terminal(Node node, int index)
        {
            ThrowIfNull(node);

            _nodeOrRegion = node;
            _index = checked((byte)index);
            var def = node.Type.Terminals[index];
            VoxelPosition = def.Position;
            SignalType = def.SignalType;
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

            _nodeOrRegion = node;
            _index = checked((byte)node.Type[name].Index);
            var def = node.Type[name];
            VoxelPosition = def.Position;
            SignalType = def.SignalType;
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
            _nodeOrRegion = node;
            _index = checked((byte)def.Index);
            VoxelPosition = def.Position;
            SignalType = def.SignalType;
        }

        private Terminal(short3 blockPosition, byte3 voxelPositon, byte index, SignalType signalType)
        {
            _blockPositon = blockPosition;
            _index = index;
            VoxelPosition = voxelPositon;
            SignalType = signalType;
        }

        private Terminal(BlockRegion region, short3 positionInRange, byte3 voxelPositon, byte index, SignalType signalType)
        {
            _nodeOrRegion = region;
            _blockPositon = positionInRange;
            _index = index;
            VoxelPosition = voxelPositon;
            SignalType = signalType;
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
                if (_nodeOrRegion is Node)
                {
                    return TerminalType.Node;
                }
                else if (_nodeOrRegion is BlockRegion)
                {
                    return TerminalType.ObjectRelative;
                }
                else if (_blockPositon.X == FromToOutsideValue)
                {
                    return _blockPositon.Y == 0 ? TerminalType.OutsideInput : TerminalType.OutsideOutput;
                }
                else
                {
                    return TerminalType.ObjectAbsolute;
                }
            }
        }

        /// <summary>
        /// Gets the node this terminal belongs to.
        /// </summary>
        /// <value>The node this terminal belongs to; or <see langword="null"/>, if <see cref="Type"/> is not <see cref="TerminalType.Node"/>.</value>
        public readonly Node? Node => _nodeOrRegion as Node;

        /// <summary>
        /// Gets the <see cref="BlockRegion"/> this terminal belongs to.
        /// </summary>
        /// <value>The <see cref="BlockRegion"/> this terminal belongs to; or <see langword="null"/>, if <see cref="Type"/> is not <see cref="TerminalType.ObjectRelative"/>.</value>
        public readonly BlockRegion? Region => _nodeOrRegion as BlockRegion;

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

        /// <summary>
        /// Creates an external input terminal.
        /// </summary>
        /// <param name="voxelPositon">Voxel position on the outside prefab.</param>
        /// <param name="index">Index of the terminal.</param>
        /// <param name="signalType">Type of the terminal.</param>
        /// <returns>The contructed terminal.</returns>
        public static Terminal CreateIn(byte3 voxelPositon, int index, SignalType signalType)
            => new Terminal(new short3(FromToOutsideValue, (short)0, (short)0), voxelPositon, checked((byte)index), signalType);

        /// <summary>
        /// Creates an external output terminal.
        /// </summary>
        /// <param name="voxelPositon">Voxel position on the outside prefab.</param>
        /// <param name="index">Index of the terminal.</param>
        /// <param name="signalType">Type of the terminal.</param>
        /// <returns>The contructed terminal.</returns>
        public static Terminal CreateOut(byte3 voxelPositon, int index, SignalType signalType)
            => new Terminal(new short3(FromToOutsideValue, (short)1, (short)0), voxelPositon, checked((byte)index), signalType);

        /// <summary>
        /// Creates an object terminal at a given block position, used to connect to non script blocks.
        /// </summary>
        /// <param name="blockPosition">Absolute position of the block to connect to.</param>
        /// <param name="voxelPositon">Voxel position on the outside prefab.</param>
        /// <returns>The contructed terminal.</returns>
        public static Terminal ObjectAbsolute(int3 blockPosition, byte3 voxelPositon)
            => new Terminal(checked((short3)blockPosition), voxelPositon, 0, SignalType.Obj);

        /// <summary>
        /// Creates an object terminal at a given block position, used to connect to non script blocks.
        /// </summary>
        /// <param name="region">The <see cref="BlockRegion"/> to which <paramref name="positionInRange"/> is relative to.</param>
        /// <param name="positionInRange">Position relative to <paramref name="region"/>.</param>
        /// <param name="voxelPositon">Voxel position on the outside prefab.</param>
        /// <returns>The contructed terminal.</returns>
        public static Terminal ObjectRelative(BlockRegion region, int3 positionInRange, byte3 voxelPositon)
            => new Terminal(region, checked((short3)positionInRange), voxelPositon, 0, SignalType.Obj);

        /// <inheritdoc/>
        public bool Equals(Terminal other)
            => Index == other.Index && _nodeOrRegion == other._nodeOrRegion && _blockPositon == other._blockPositon && VoxelPosition == other.VoxelPosition && SignalType == other.SignalType;

        /// <inheritdoc/>
        public override bool Equals(object? obj)
            => obj is Terminal terminal && Equals(terminal);

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(RuntimeHelpers.GetHashCode(_nodeOrRegion), _blockPositon, _index, VoxelPosition, SignalType);
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
        public TerminalStore(Terminal @in, OutTerminalsEnumerable @out)
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
        /// Gets the input terminal.
        /// </summary>
        /// <value>The input terminal.</value>
        public Terminal In { get; }

        /// <summary>
        /// Gets the output terminals.
        /// </summary>
        /// <value>An enumerable collection of output terminals.</value>
        public OutTerminalsEnumerable Out => new OutTerminalsEnumerable(_outTerminals);

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
        /// Provides an allocation-free enumerable over a collection of output terminals.
        /// </summary>
        [StructLayout(LayoutKind.Auto)]
        public readonly struct OutTerminalsEnumerable : IEnumerable<Terminal>
        {
            internal readonly TerminalsOutCollection _outTerminals;

            internal OutTerminalsEnumerable(TerminalsOutCollection outTerminals)
            {
                _outTerminals = outTerminals;
            }

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

    /// <summary>
    /// Provides an allocation-free enumerable over a collection of <see cref="PrefabSetting"/>s.
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    public readonly struct SettingsEnumerable : IEnumerable<PrefabSetting>
    {
        private readonly Node _node;

        internal SettingsEnumerable(Node node)
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