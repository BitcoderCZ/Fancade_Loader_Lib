// <copyright file="PooledStackPolicy.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using Microsoft.Extensions.ObjectPool;

namespace BitcoderCZ.Fancade.Runtime.Simulated.Utils;

internal sealed class PooledStackPolicy<T> : IPooledObjectPolicy<Stack<T>>
{
    public int DefaultCapacity { get; init; } = 16;

#if NET9_0_OR_GREATER
    public int MaxCapacity { get; init; } = int.MaxValue;
#endif

    public Stack<T> Create()
        => new Stack<T>(DefaultCapacity);

    public bool Return(Stack<T> obj)
    {
        obj.Clear();
#if NET9_0_OR_GREATER
        return obj.Capacity <= MaxCapacity;
#else
        return true;
#endif
    }
}