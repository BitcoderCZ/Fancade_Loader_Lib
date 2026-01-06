// <copyright file="ValueListWithHash.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace BitcoderCZ.Fancade.Runtime.Simulated.Utils;

[StructLayout(LayoutKind.Auto)]
public struct ValueListWithHash<T> : IEquatable<ValueListWithHash<T>>
    where T : unmanaged, IEquatable<T>
{
    public ValueList<T> List;
    private int? _hashCode;

    public void ComputeHash()
        => _hashCode = List.CalculateHashCode();

    public readonly bool Equals(ValueListWithHash<T> other)
        => _hashCode == other._hashCode && List.SequenceEqual(in other.List);

    public override int GetHashCode()
        => _hashCode ??= List.CalculateHashCode();

    public override readonly bool Equals([NotNullWhen(true)] object? obj)
        => obj is ValueListWithHash<T> other && Equals(other);
}