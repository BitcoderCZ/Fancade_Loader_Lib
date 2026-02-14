// <copyright file="FcKeys.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace BitcoderCZ.Fancade.Runtime.Input;

/// <summary>
/// Stores the state of keys.
/// </summary>
public sealed class FcKeys : IReadOnlyDictionary<FcKeyCode, bool>
{
    /// <summary>
    /// The amount of keys.
    /// </summary>
#pragma warning disable CS0612 // Type or member is obsolete
    public static readonly int Count = (int)FcKeyCode.LastValue;
#pragma warning restore CS0612 // Type or member is obsolete

    private readonly bool[] _pressed = new bool[Count];

    /// <inheritdoc/>
    IEnumerable<FcKeyCode> IReadOnlyDictionary<FcKeyCode, bool>.Keys => Enumerable.Range(0, Count).Select(i => (FcKeyCode)i);

    /// <inheritdoc/>
    IEnumerable<bool> IReadOnlyDictionary<FcKeyCode, bool>.Values => _pressed;

    /// <inheritdoc/>
    int IReadOnlyCollection<KeyValuePair<FcKeyCode, bool>>.Count => Count;

    /// <inheritdoc/>
    public bool this[FcKeyCode key]
    {
        get => _pressed[(int)key];
        set => _pressed[(int)key] = value;
    }

    /// <summary>
    /// Copies the elements of the <see cref="FcKeys"/> to a <see cref="Span{T}"/>.
    /// </summary>
    /// <param name="destination">The destination <see cref="Span{T}"/>.</param>
    public void CopyTo(Span<bool> destination)
        => _pressed.AsSpan().CopyTo(destination);

    /// <summary>
    /// Copies the elements of the <see cref="FcKeys"/> to a <see cref="Span{T}"/>.
    /// </summary>
    /// <param name="destination">The destination <see cref="Span{T}"/>.</param>
    public void CopyTo(Span<KeyValuePair<FcKeyCode, bool>> destination)
    {
        for (int i = 0; i < Count; i++)
        {
            destination[i] = new((FcKeyCode)i, _pressed[i]);
        }
    }

    /// <summary>
    /// Copies the elements of the <see cref="FcKeys"/> to another <see cref="FcKeys"/>.
    /// </summary>
    /// <param name="destination">The other <see cref="FcKeys"/>.</param>
    public void CopyTo(FcKeys destination)
        => _pressed.AsSpan().CopyTo(destination._pressed);

    /// <summary>
    /// Marks all keys as not pressed.
    /// </summary>
    public void Clear()
        => _pressed.AsSpan().Clear();

    /// <inheritdoc/>
    bool IReadOnlyDictionary<FcKeyCode, bool>.ContainsKey(FcKeyCode key)
#pragma warning disable CS0612 // Type or member is obsolete
        => key is >= 0 and < FcKeyCode.LastValue;
#pragma warning restore CS0612 // Type or member is obsolete

    /// <inheritdoc/>
    bool IReadOnlyDictionary<FcKeyCode, bool>.TryGetValue(FcKeyCode key, [MaybeNullWhen(false)] out bool value)
    {
#pragma warning disable CS0612 // Type or member is obsolete
        if (key is >= 0 and < FcKeyCode.LastValue)
#pragma warning restore CS0612 // Type or member is obsolete
        {
            value = this[key];
            return true;
        }

        value = default;
        return false;
    }

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<FcKeyCode, bool>> GetEnumerator()
        => ((IReadOnlyDictionary<FcKeyCode, bool>)this).Keys.Zip(_pressed, (a, b) => new KeyValuePair<FcKeyCode, bool>(a, b)).GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}