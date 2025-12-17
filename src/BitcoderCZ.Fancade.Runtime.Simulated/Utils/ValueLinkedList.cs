// <copyright file="ValueLinkedList.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static BitcoderCZ.Fancade.Utils.ThrowHelper;

namespace BitcoderCZ.Fancade.Runtime.Simulated.Utils;

internal struct ValueLinkedList<T> : IEnumerable<T>
{
    private const int MaxItemsPerNode = 8;

    private ValueCollection8 _firstData;
    private Node? _firstNode;

    public void Add(T item)
    {
        if (_firstData.Count < MaxItemsPerNode)
        {
            _firstData.Add(item);
            return;
        }

        if (_firstNode is null)
        {
            _firstNode = new Node();
            _firstNode.Data.Add(item);
            return;
        }

        // find last node
        Node current = _firstNode;
        while (current.NextNode is not null)
        {
            current = current.NextNode;
        }

        if (current.Count < MaxItemsPerNode)
        {
            current.Data.Add(item);
        }
        else
        {
            var newNode = new Node();
            newNode.Data.Add(item);
            current.NextNode = newNode;
        }
    }

    public readonly Enumerator GetEnumerator()
        => new Enumerator(this);

    readonly IEnumerator<T> IEnumerable<T>.GetEnumerator()
        => GetEnumerator();

    readonly IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public struct Enumerator : IEnumerator<T>
    {
        private readonly ValueLinkedList<T> _collection;
        private Node? _node;
        private int _index;
        private T _current;

        internal Enumerator(ValueLinkedList<T> collection)
        {
            _collection = collection;
            _node = null;
            _index = -1;
            _current = default!;
        }

        /// <inheritdoc/>
        public readonly T Current => _current;

        /// <inheritdoc/>
        readonly object IEnumerator.Current => Current!;

        /// <inheritdoc/>
        public bool MoveNext()
        {
            _index++;

            if (_index < (_node is null ? _collection._firstData.Count : _node.Count))
            {
                if (_node is null)
                {
                    _current = _collection._firstData[_index];
                }
                else
                {
                    _current = _node.Data[_index];
                }

                return true;
            }

            if (_index < MaxItemsPerNode)
            {
                // Since a new node is only created when the last one is full, and we know that the index is out of bounds of the current one, if it is not out of bounds of the maximum capacity, we know that the current node is not full, meaning that the current node is the last node
                return false;
            }

            if (_node is null)
            {
                if (_collection._firstNode is null)
                {
                    return false;
                }

                _node = _collection._firstNode;
            }
            else
            {
                if (_node.NextNode is null)
                {
                    return false;
                }

                _node = _node.NextNode;
            }

            _index = 0;
            _current = _node.Data[0];
            return true;
        }

        /// <inheritdoc/>
        public void Reset()
        {
            _node = null;
            _index = -1;
            _current = default!;
        }

        /// <inheritdoc/>
        public readonly void Dispose()
        {
            // empty
        }
    }

    private struct ValueCollection8
    {
        private byte _count;
        private Array8 _data;

        public ValueCollection8()
        {
            _count = 0;
            _data = default;
        }

        public readonly int Count => _count;

        [UnscopedRef]
        private readonly Span<T> Data => Array8.AsSpan(ref Unsafe.AsRef(in _data));

        public readonly T this[int index]
        {
            get
            {
                ThrowIfGreaterThanOrEqualToOrNegative(index, Count, nameof(index));

                return Data[index];
            }
        }

        public void Add(T item)
        {
            if (_count == MaxItemsPerNode)
            {
                ThrowInvalidOperationException();
            }

            Data[_count++] = item;
        }
    }

#if NET8_0_OR_GREATER
    [InlineArray(MaxItemsPerNode)]
    private struct Array8
    {
        private T _element0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan(ref Array8 array)
            => array;
    }
#else
    [StructLayout(LayoutKind.Sequential)]
    private struct Array8
    {
        public T _element0;
        public T _element1;
        public T _element2;
        public T _element3;
        public T _element4;
        public T _element5;
        public T _element6;
        public T _element7;

        public Array8(T element0, T element1, T element2, T element3, T element4, T element5, T element6, T element7)
        {
            _element0 = element0;
            _element1 = element1;
            _element2 = element2;
            _element3 = element3;
            _element4 = element4;
            _element5 = element5;
            _element6 = element6;
            _element7 = element7;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan(ref Array8 array)
            => MemoryMarshal.CreateSpan(ref array._element0, MaxItemsPerNode);
    }
#endif

    private sealed class Node
    {
        public ValueCollection8 Data;
        public Node? NextNode;

        public int Count => Data.Count;
    }
}
