using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static BitcoderCZ.Fancade.Utils.ThrowHelper;

namespace BitcoderCZ.Fancade.Runtime.Simulated.Utils;

internal struct ValueList<T> : IList<T>, IReadOnlyList<T>
    where T : IEquatable<T>
{
    private const int BufferCapacity = 8;
    private const int ListStartCapacity = 4;

    private int _count;
    private Buffer8 _buffer = default;
    private List<T>? _list;

    public ValueList(int capacity)
    {
        _count = 0;
        _list = capacity > BufferCapacity ? new List<T>(capacity - BufferCapacity) : null;
    }

    public ValueList(IEnumerable<T> collection)
    {
        _count = 0;

        if (collection is ICollection<T> c)
        {
            int count = c.Count;
            _list = count > BufferCapacity ? new List<T>(count - BufferCapacity) : null;
        }
        else if (collection is IReadOnlyCollection<T> roc)
        {
            int count = roc.Count;
            _list = count > BufferCapacity ? new List<T>(count - BufferCapacity) : null;
        }
        else
        {
            _list = null;
        }

        foreach (var item in collection)
        {
            Add(item);
        }
    }

    public readonly int Count => _count;

    public int Capacity
    {
        readonly get => _list is null ? BufferCapacity : BufferCapacity + _list.Capacity;
        set
        {
            ThrowIfNegative(value, nameof(value));

            if (value <= BufferCapacity)
            {
                _list = null;
            }
            else if (_list is null)
            {
                _list = new List<T>(value - BufferCapacity);
            }
            else
            {
                _list.Capacity = value - BufferCapacity;
            }
        }
    }

    public readonly bool IsReadOnly => false;

    [UnscopedRef]
#pragma warning disable IDE0251 // Make member 'readonly'
    private Span<T> BufferSpan => Buffer8.AsSpan(ref Unsafe.AsRef(in _buffer));
#pragma warning restore IDE0251 // Make member 'readonly'

    [UnscopedRef]
    private readonly ReadOnlySpan<T> ROBufferSpan => Buffer8.AsSpan(ref Unsafe.AsRef(in _buffer));

    public T this[int index]
    {
        readonly get
        {
            ThrowIfGreaterThanOrEqualToOrNegative(index, Count, nameof(index));

            return index < BufferCapacity
                ? ROBufferSpan[index]
                : _list![index - BufferCapacity];
        }

        set
        {
            ThrowIfGreaterThanOrEqualToOrNegative(index, Count, nameof(index));

            if (index < BufferCapacity)
            {
                BufferSpan[index] = value;
            }
            else
            {
                _list![index - BufferCapacity] = value;
            }
        }
    }

    public void Add(T item)
    {
        if (_count < BufferCapacity)
        {
            BufferSpan[_count++] = item;
            return;
        }

        if (_list is null)
        {
            _list = new List<T>(ListStartCapacity);
        }

        _list.Add(item);
        _count++;
    }

    public void Clear()
    {
        BufferSpan.Clear();
        _list?.Clear();
        _count = 0;
    }

    public readonly bool Contains(T item)
        => IndexOf(item) >= 0;

    public readonly void CopyTo(T[] array, int arrayIndex)
    {
        if (array.Length - arrayIndex < _count)
        {
            ThrowArgumentOutOfRangeException(nameof(array), $"{nameof(array)} is not large enough.");
        }

        var bufferSpan = ROBufferSpan;
        for (int i = 0; i < Math.Min(_count, BufferCapacity); i++)
        {
            array[i + arrayIndex] = bufferSpan[i];
        }

        if (_list is not null)
        {
            CollectionsMarshal.AsSpan(_list)[..(_count - BufferCapacity)].CopyTo(array.AsSpan(arrayIndex + BufferCapacity));
        }
    }

    public readonly int IndexOf(T item)
    {
        if (_count == 0)
        {
            return -1;
        }

        var bufferSpan = ROBufferSpan;
        for (int i = 0; i < Math.Min(_count, BufferCapacity); i++)
        {
#pragma warning disable HAM0001 // Operation causes the compiler to create a defensive copy
            if (bufferSpan[i].Equals(item))
#pragma warning restore HAM0001 // Operation causes the compiler to create a defensive copy
            {
                return i;
            }
        }

        if (_list is not null && _count > BufferCapacity)
        {
            int index = CollectionsMarshal.AsSpan(_list)[..(_count - BufferCapacity)].IndexOf(item);
            return index == -1 ? -1 : index + BufferCapacity;
        }

        return -1;
    }

    public void Insert(int index, T item)
    {
        ThrowIfGreaterThanOrEqualToOrNegative(index, Count, nameof(index));

        if (index == _count)
        {
            Add(item);
            return;
        }

        var bufferSpan = BufferSpan;
        if (_count < BufferCapacity)
        {
            for (int i = _count; i > index; i--)
            {
                bufferSpan[i] = bufferSpan[i - 1];
            }

            bufferSpan[index] = item;
            _count++;
            return;
        }

        if (_list is null)
        {
            _list = new List<T>(ListStartCapacity);
        }

        if (index < BufferCapacity)
        {
            T lastBuffer = bufferSpan[BufferCapacity - 1];
            for (int i = BufferCapacity - 1; i > index; i--)
            {
                bufferSpan[i] = bufferSpan[i - 1];
            }

            bufferSpan[index] = item;

            _list.Insert(0, lastBuffer);
        }
        else
        {
            _list.Insert(index - BufferCapacity, item);
        }

        _count++;
    }

    public bool Remove(T item)
    {
        int index = IndexOf(item);
        if (index >= 0)
        {
            RemoveAt(index);
            return true;
        }

        return false;
    }

    public void RemoveAt(int index)
    {
        ThrowIfGreaterThanOrEqualToOrNegative(index, _count, nameof(index));

        var bufferSpan = BufferSpan;
        if (index < BufferCapacity)
        {
            for (int i = index; i < Math.Min(_count, BufferCapacity) - 1; i++)
            {
                bufferSpan[i] = bufferSpan[i + 1];
            }

            if (_count > BufferCapacity)
            {
                Debug.Assert(_list is not null, $"{nameof(_list)} should not be null.");

                bufferSpan[BufferCapacity - 1] = _list[0];
                _list.RemoveAt(0);
            }
        }
        else
        {
            Debug.Assert(_list is not null, $"{nameof(_list)} should not be null.");

            _list.RemoveAt(index - BufferCapacity);
        }

        _count--;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Enumerator GetEnumerator()
        => new Enumerator(this);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    readonly IEnumerator<T> IEnumerable<T>.GetEnumerator()
        => GetEnumerator();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    readonly IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public struct Enumerator : IEnumerator<T>
    {
        private readonly ValueList<T> _list;

        private int _index;
        private T _current;

        internal Enumerator(ValueList<T> list)
        {
            _list = list;
            _index = -1;
            _current = default!;
        }

        public readonly T Current => _current;

        readonly object? IEnumerator.Current
        {
            get
            {
                if (_index <= 0)
                {
                    ThrowIndexArgumentOutOfRange();
                }

                return _current;
            }
        }

        public bool MoveNext()
        {
            _index++;

            if ((uint)_index < (uint)_list.Count)
            {
                _current = _index < BufferCapacity
                    ? _list.ROBufferSpan[_index]
                    : _list._list![_index - BufferCapacity];

                return true;
            }

            _current = default!;
            _index = -1;
            return false;
        }

        void IEnumerator.Reset()
        {
            _index = -1;
            _current = default!;
        }

        public readonly void Dispose()
        {
        }
    }

#if NET8_0_OR_GREATER
    [InlineArray(BufferCapacity)]
    private struct Buffer8
    {
        private T _element0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan(ref Buffer8 array)
            => array;
    }
#else
    [StructLayout(LayoutKind.Sequential)]
    private struct Buffer8
    {
        public T _element0;
        public T _element1;
        public T _element2;
        public T _element3;
        public T _element4;
        public T _element5;
        public T _element6;
        public T _element7;

        public Buffer8(T element0, T element1, T element2, T element3, T element4, T element5, T element6, T element7)
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
        public static Span<T> AsSpan(ref Buffer8 array)
            => MemoryMarshal.CreateSpan(ref array._element0, BufferCapacity);
    }
#endif
}
