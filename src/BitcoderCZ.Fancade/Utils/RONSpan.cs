// <copyright file="RONSpan.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using BitcoderCZ.Utils;

namespace BitcoderCZ.Fancade.Utils;

#pragma warning disable SA1201 // Elements should appear in the correct order
internal readonly unsafe struct RONSpan<T>
    where T : unmanaged
{
    /// <summary>A byref or a native ptr.</summary>
    internal readonly T* _reference;

    /// <summary>The number of elements this ReadOnlySpan contains.</summary>
    private readonly int _length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RONSpan(T* reference, int length)
    {
        ThrowHelper.ThrowIfNegative(length);

        _reference = reference;
        _length = length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RONSpan(T* reference)
    {
        _reference = reference;
        _length = 1;
    }

    /// <summary>
    /// Returns the specified element of the read-only span.
    /// </summary>
    /// <param name="index">The zero-based index.</param>
    /// <exception cref="IndexOutOfRangeException">
    /// Thrown when index less than 0 or index greater than or equal to Length.
    /// </exception>
    public ref readonly T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if ((uint)index >= (uint)_length)
            {
                ThrowHelper.ThrowIndexArgumentOutOfRange();
            }

            return ref Unsafe.Add(ref *_reference, (nint)(uint)index /* force zero-extension */);
        }
    }

    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _length;
    }

    /// <summary>
    /// Gets a value indicating whether this <see cref="RONSpan{T}"/> is empty.
    /// </summary>
    /// <value><see langword="true"/> if this span is empty; otherwise, <see langword="false"/>.</value>
    public bool IsEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _length == 0;
    }

    /// <summary>
    /// Returns false if left and right point at the same memory and have the same length.  Note that
    /// this does *not* check to see if the *contents* are equal.
    /// </summary>
    public static bool operator !=(RONSpan<T> left, RONSpan<T> right) => !(left == right);

    /// <summary>
    /// This method is not supported as spans cannot be boxed. To compare two spans, use operator==.
    /// </summary>
    /// <exception cref="NotSupportedException">
    /// Always thrown by this method.
    /// </exception>
    [Obsolete("Equals() on ReadOnlySpan will always throw an exception. Use the equality operator instead.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member
    public override bool Equals(object? obj)
#pragma warning restore CS0809 // Obsolete member overrides non-obsolete member
        => throw new NotSupportedException();

    /// <summary>
    /// This method is not supported as spans cannot be boxed.
    /// </summary>
    /// <exception cref="NotSupportedException">
    /// Always thrown by this method.
    /// </exception>
    [Obsolete("GetHashCode() on ReadOnlySpan will always throw an exception.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member
    public override int GetHashCode()
#pragma warning restore CS0809 // Obsolete member overrides non-obsolete member
        => throw new NotSupportedException();

    /// <summary>
    /// Gets a 0-length read-only span whose base is the null pointer.
    /// </summary>
    public static RONSpan<T> Empty => default;

    /// <summary>Gets an enumerator for this span.</summary>
    public Enumerator GetEnumerator() => new Enumerator(this);

    /// <summary>Enumerates the elements of a <see cref="ReadOnlySpan{T}"/>.</summary>
    public struct Enumerator : IEnumerator<T>
    {
        /// <summary>The span being enumerated.</summary>
        private readonly RONSpan<T> _span;

        /// <summary>The next index to yield.</summary>
        private int _index;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Enumerator(RONSpan<T> span)
        {
            _span = span;
            _index = -1;
        }

        /// <summary>Advances the enumerator to the next element of the span.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            int index = _index + 1;
            if (index < _span.Length)
            {
                _index = index;
                return true;
            }

            return false;
        }

        /// <summary>Gets the element at the current position of the enumerator.</summary>
        public readonly ref readonly T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref _span[_index];
        }

        /// <inheritdoc />
        readonly T IEnumerator<T>.Current => Current;

        /// <inheritdoc />
        readonly object IEnumerator.Current => Current!;

        /// <inheritdoc />
        void IEnumerator.Reset() => _index = -1;

        /// <inheritdoc />
        readonly void IDisposable.Dispose()
        {
        }
    }

    /// <summary>
    /// Returns a reference to the 0th element of the Span. If the Span is empty, returns null reference.
    /// It can be used for pinning and is required to support the use of span within a fixed statement.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public ref readonly T GetPinnableReference()
    {
        // Ensure that the native code has just one forward branch that is predicted-not-taken.
        ref T ret = ref Unsafe.NullRef<T>();
        if (_length != 0)
        {
            ret = ref *_reference;
        }

        return ref ret;
    }

    /// <summary>
    /// Copies the contents of this read-only span into destination span. If the source
    /// and destinations overlap, this method behaves as if the original values in
    /// a temporary location before the destination is overwritten.
    /// </summary>
    /// <param name="destination">The span to copy items into.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when the destination Span is shorter than the source Span.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(Span<T> destination)
        => AsSpan().CopyTo(destination);

    /// <summary>
    /// Copies the contents of this read-only span into destination span. If the source
    /// and destinations overlap, this method behaves as if the original values in
    /// a temporary location before the destination is overwritten.
    /// </summary>
    /// <returns>If the destination span is shorter than the source span, this method
    /// return false and no data is written to the destination.</returns>
    /// <param name="destination">The span to copy items into.</param>
    public bool TryCopyTo(Span<T> destination)
        => AsSpan().TryCopyTo(destination);

    /// <summary>
    /// Returns true if left and right point at the same memory and have the same length.  Note that
    /// this does *not* check to see if the *contents* are equal.
    /// </summary>
    public static bool operator ==(RONSpan<T> left, RONSpan<T> right)
        => left._length == right._length && left._reference == right._reference;

    public ReadOnlySpan<T> AsSpan()
        => new ReadOnlySpan<T>(_reference, _length);

    /// <summary>
    /// For <see cref="RONSpan{Char}"/>, returns a new instance of string that represents the characters pointed to by the span.
    /// Otherwise, returns a <see cref="string"/> with the name of the type and the number of elements.
    /// </summary>
    public override string ToString()
        => AsSpan().ToString();

    /// <summary>
    /// Forms a slice out of the given read-only span, beginning at 'start'.
    /// </summary>
    /// <param name="start">The zero-based index at which to begin this slice.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the specified <paramref name="start"/> index is not in range (&lt;0 or &gt;Length).
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<T> Slice(int start)
    {
        if ((uint)start > (uint)_length)
        {
            ThrowHelper.ThrowArgumentOutOfRangeException();
        }

        return new ReadOnlySpan<T>(_reference + (nint)(uint)start, _length - start);
    }

    /// <summary>
    /// Forms a slice out of the given read-only span, beginning at 'start', of given length.
    /// </summary>
    /// <param name="start">The zero-based index at which to begin this slice.</param>
    /// <param name="length">The desired length for the slice (exclusive).</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the specified <paramref name="start"/> or end index is not in range (&lt;0 or &gt;Length).
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<T> Slice(int start, int length)
    {
#if TARGET_64BIT
        // See comment in Span<T>.Slice for how this works.
        if ((ulong)(uint)start + (ulong)(uint)length > (ulong)(uint)_length)
        {
            ThrowHelper.ThrowArgumentOutOfRangeException();
        }
#else
        if ((uint)start > (uint)_length || (uint)length > (uint)(_length - start))
        {
            ThrowHelper.ThrowArgumentOutOfRangeException();
        }
#endif

        return new ReadOnlySpan<T>(_reference + (nint)(uint)start /* force zero-extension */, length);
    }

    /// <summary>
    /// Copies the contents of this read-only span into a new array.  This heap
    /// allocates, so should generally be avoided, however it is sometimes
    /// necessary to bridge the gap with APIs written in terms of arrays.
    /// </summary>
    public T[] ToArray()
        => AsSpan().ToArray();
}