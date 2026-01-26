// <copyright file="FcMesh.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BitcoderCZ.Fancade.Runtime.Simulated.Utils;
using BitcoderCZ.Maths.Vectors;
using BitcoderCZ.Utils;

namespace BitcoderCZ.Fancade.Runtime.Simulated;

[DebuggerTypeProxy(typeof(FcMeshDebugView))]
[DebuggerDisplay("Position = {Position}; Count = {Count}")]
[StructLayout(LayoutKind.Auto)]
public struct FcMesh : IEquatable<FcMesh>, IReadOnlyList<FcMesh.Block>
{
    internal ValueList<Block> Blocks;
    private int? _hashCode;

    internal FcMesh(ValueList<Block> blocks, int3 position)
    {
        Blocks = blocks;
        Position = position;
    }

    /// <summary>
    /// Gets the position of the mesh.
    /// </summary>
    /// <value>Position of the mesh.</value>
    public readonly int3 Position { get; }

    /// <summary>
    /// Gets the absolute positions of the blocks, may contain duplicates.
    /// </summary>
    /// <value>Absolute positions of the blocks, may contain duplicates.</value>
    public readonly PositionsEnumerable Positions => new PositionsEnumerable(Position, Blocks);

    /// <inheritdoc/>
    public readonly int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Blocks.Count;
    }

    internal int HasCode
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _hashCode ??= Blocks.CalculateHashCode();
    }

    /// <inheritdoc/>
    public readonly Block this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Blocks[index];
    }

    /// <summary>Returns a value that indicates whether the 2 <see cref="Block"/>s are equal.</summary>
    /// <param name="left">The first <see cref="Block"/> to compare.</param>
    /// <param name="right">The second <see cref="Block"/> to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(FcMesh left, FcMesh right)
        => left.Equals(right);

    /// <summary>Returns a value that indicates whether the 2 <see cref="Block"/>s are not equal.</summary>
    /// <param name="left">The first <see cref="Block"/> to compare.</param>
    /// <param name="right">The second <see cref="Block"/> to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(FcMesh left, FcMesh right)
        => !(left == right);

    /// <inheritdoc/>
    public bool Equals(FcMesh other)
        => HasCode == other.HasCode && Position == other.Position && Blocks.SequenceEqual(in other.Blocks);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj)
        => obj is FcMesh && Equals((FcMesh)obj);

    /// <inheritdoc/>
    public override int GetHashCode()
        => HashCode.Combine(HasCode, Position);

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the collection.</returns>
    public readonly ValueList<Block>.Enumerator GetEnumerator()
        => Blocks.GetEnumerator();

    /// <inheritdoc/>
    readonly IEnumerator<Block> IEnumerable<Block>.GetEnumerator()
        => GetEnumerator();

    /// <inheritdoc/>
    readonly IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    [DebuggerDisplay("SegmentId = {SegmentId}; Offset = {Offset}; LocalMeshIndex = {LocalMeshIndex}")]
    [StructLayout(LayoutKind.Auto)]
    public readonly struct Block : IEquatable<Block>, IComparable<Block>
    {
        public Block(ushort segmentId, int3 offset, ushort localMeshIndex)
        {
            SegmentId = segmentId;
            Offset = offset;
            LocalMeshIndex = localMeshIndex;
        }

        public readonly ushort SegmentId { get; }

        /// <summary>
        /// Gets the offset of the block.
        /// </summary>
        /// <value>Offset of the block relative to <see cref="FcMesh.Position"/>.</value>
        public readonly int3 Offset { get; }

        public readonly ushort LocalMeshIndex { get; }

        /// <summary>Returns a value that indicates whether the 2 <see cref="Block"/>s are equal.</summary>
        /// <param name="left">The first <see cref="Block"/> to compare.</param>
        /// <param name="right">The second <see cref="Block"/> to compare.</param>
        /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are equal; otherwise, <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Block left, Block right)
            => left.Equals(right);

        /// <summary>Returns a value that indicates whether the 2 <see cref="Block"/>s are not equal.</summary>
        /// <param name="left">The first <see cref="Block"/> to compare.</param>
        /// <param name="right">The second <see cref="Block"/> to compare.</param>
        /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are not equal; otherwise, <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Block left, Block right)
            => !(left == right);

        /// <inheritdoc/>
        public int CompareTo(Block other)
        {
            int comp = SegmentId.CompareTo(other.SegmentId);
            if (comp != 0)
            {
                return comp;
            }

            comp = LocalMeshIndex.CompareTo(other.LocalMeshIndex);
            if (comp != 0)
            {
                return comp;
            }

            comp = Offset.Z.CompareTo(other.Offset.Z);
            if (comp != 0)
            {
                return comp;
            }

            comp = Offset.Y.CompareTo(other.Offset.Y);
            return comp != 0 ? comp : Offset.X.CompareTo(other.Offset.X);
        }

        /// <inheritdoc/>
        public bool Equals(Block other)
            => SegmentId == other.SegmentId && LocalMeshIndex == other.LocalMeshIndex && Offset == other.Offset;

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object? obj)
            => obj is Block && Equals((Block)obj);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
            => HashCode.Combine(SegmentId, Offset, LocalMeshIndex);
    }

    public readonly struct PositionsEnumerable : IEnumerable<int3>
    {
        private readonly int3 _position;
        private readonly ValueList<Block> _blocks;

        internal PositionsEnumerable(int3 position, ValueList<Block> blocks)
        {
            _position = position;
            _blocks = blocks;
        }

        public readonly PositionsEnumerator GetEnumerator()
            => new PositionsEnumerator(_position, _blocks.GetEnumerator());

        readonly IEnumerator<int3> IEnumerable<int3>.GetEnumerator()
            => GetEnumerator();

        readonly IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }

    public struct PositionsEnumerator : IEnumerator<int3>
    {
        private readonly int3 _position;
        private ValueList<Block>.Enumerator _blocks;

        internal PositionsEnumerator(int3 position, ValueList<Block>.Enumerator blocks)
        {
            _position = position;
            _blocks = blocks;
        }

        public readonly int3 Current => _blocks.Current.Offset + _position;

        readonly object IEnumerator.Current => Current;

        public bool MoveNext()
            => _blocks.MoveNext();

        void IEnumerator.Reset()
            => ((IEnumerator)_blocks).Reset();

        readonly void IDisposable.Dispose()
        {
        }
    }

    public sealed class Builder
    {
        private ValueList<Block> _blocks;

        public Builder()
        {
        }

        public Builder(int capacity)
        {
            _blocks = new(capacity);
        }

        public Builder Add(Block block)
        {
            _blocks.Add(block);
            return this;
        }

        public void Clear()
            => _blocks.Clear();

        public void Drain(out FcMesh mesh)
        {
            int3 minPos = new int3(int.MaxValue, int.MaxValue, int.MaxValue);
            foreach (var block in _blocks)
            {
                minPos = int3.Min(minPos, block.Offset);
            }

            for (int i = _blocks.Count - 1; i >= 0; i--)
            {
                ref var block = ref _blocks.GetRef(i);
                block = new Block(block.SegmentId, block.Offset - minPos, block.LocalMeshIndex);
            }

            _blocks.Sort();
            mesh = new FcMesh(_blocks, minPos);

            _blocks = default;
        }
    }

    /// <summary>
    /// An <see cref="IEqualityComparer{T}"/> for <see cref="FcMesh"/> that only compares blocks (ignores <see cref="FcMesh.Position"/>).
    /// </summary>
    public sealed class BlocksEqualityComparer : IEqualityComparer<FcMesh>
    {
        public static readonly BlocksEqualityComparer Instance = new();

        private BlocksEqualityComparer()
        {
        }

        /// <inheritdoc/>
        public bool Equals(FcMesh x, FcMesh y)
            => x.HasCode == y.HasCode && x.Blocks.SequenceEqual(in y.Blocks);

        /// <inheritdoc/>
        public int GetHashCode([DisallowNull] FcMesh obj)
            => obj.HasCode;
    }
}

internal sealed class FcMeshDebugView
{
    private readonly FcMesh _mesh;

    public FcMeshDebugView(FcMesh mesh)
    {
        ThrowHelper.ThrowIfNull(mesh);

        _mesh = mesh;
    }

    public int3 Position => _mesh.Position;

    public FcMesh.Block[] Blocks => [.. _mesh.Blocks];
}