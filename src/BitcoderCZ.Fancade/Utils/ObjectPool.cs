// <copyright file="ObjectPool.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// based on https://github.com/dotnet/roslyn/blob/main/src/Dependencies/PooledObjects/ObjectPool%601.cs
#nullable enable

// define TRACE_LEAKS to get additional diagnostics that can lead to the leak sources. note: it will
// make everything about 2-3x slower
// 
// #define TRACE_LEAKS

// define DETECT_LEAKS to detect possible leaks
// #if DEBUG
// #define DETECT_LEAKS  //for now always enable DETECT_LEAKS in debug.
// #endif
using System;
using System.Diagnostics;
using System.Threading;

#if DETECT_LEAKS
using System.Runtime.CompilerServices;
#endif

namespace BitcoderCZ.Fancade.Utils;

/// <summary>
/// Generic implementation of object pooling pattern with predefined pool size limit. The main
/// purpose is that limited number of frequently used objects can be kept in the pool for
/// further recycling.
/// 
/// Notes: 
/// 1) it is not the goal to keep all returned objects. Pool is not meant for storage. If there
///    is no space in the pool, extra returned objects will be dropped.
/// 
/// 2) it is implied that if object was obtained from a pool, the caller will return it back in
///    a relatively short time. Keeping checked out objects for long durations is ok, but 
///    reduces usefulness of pooling. Just new up your own.
/// 
/// Not returning objects to the pool in not detrimental to the pool's work, but is a bad practice. 
/// Rationale: 
///    If there is no intent for reusing the object, do not use pool - just use "new". 
/// </summary>
internal class ObjectPool<T>
    where T : class
{
    public readonly bool TrimOnFree;

    // Storage for the pool objects. The first item is stored in a dedicated field because we
    // expect to be able to satisfy most requests from it.
    private T? _firstItem;
#pragma warning disable SA1214 // Readonly fields should appear before non-readonly fields
    private readonly Element[] _items;
#pragma warning restore SA1214 // Readonly fields should appear before non-readonly fields

    // factory is stored for the lifetime of the pool. We will call this only when pool needs to
    // expand. compared to "new T()", Func gives more flexibility to implementers and faster
    // than "new T()".
    private readonly Func<T> _factory;

    private readonly Action<T>? _clear;

#if DETECT_LEAKS
    private static readonly ConditionalWeakTable<T, LeakTracker> leakTrackers = new ConditionalWeakTable<T, LeakTracker>();
#endif

#pragma warning disable SA1201 // Elements should appear in the correct order
    public ObjectPool(Func<T> factory, Action<T>? clear = null, bool trimOnFree = true)
#pragma warning restore SA1201 // Elements should appear in the correct order
        : this(factory, clear, Environment.ProcessorCount * 2, trimOnFree)
    {
    }

    public ObjectPool(Func<T> factory, Action<T>? clear, int size, bool trimOnFree = true)
    {
        Debug.Assert(size >= 1);
        _factory = factory;
        _clear = clear;
        _items = new Element[size - 1];
        TrimOnFree = trimOnFree;
    }

    public ObjectPool(Func<ObjectPool<T>, T> factory, Action<T>? clear, int size)
    {
        Debug.Assert(size >= 1);
        _factory = () => factory(this);
        _clear = clear;
        _items = new Element[size - 1];
    }

    /// <summary>
    /// Produces an instance.
    /// </summary>
    /// <remarks>
    /// Search strategy is a simple linear probing which is chosen for it cache-friendliness.
    /// Note that Free will try to store recycled objects close to the start thus statistically 
    /// reducing how far we will typically search.
    /// </remarks>
    internal T Allocate()
    {
        // PERF: Examine the first element. If that fails, AllocateSlow will look at the remaining elements.
        // Note that the initial read is optimistically not synchronized. That is intentional. 
        // We will interlock only when we have a candidate. in a worst case we may miss some
        // recently returned objects. Not a big deal.
        var inst = _firstItem;
        if (inst == null || inst != Interlocked.CompareExchange(ref _firstItem, null, inst))
        {
            inst = AllocateSlow();
        }

#if DETECT_LEAKS
        var tracker = new LeakTracker();
        leakTrackers.Add(inst, tracker);

#if TRACE_LEAKS
        var frame = CaptureStackTrace();
        tracker.Trace = frame;
#endif
#endif
        return inst;
    }

    /// <summary>
    /// Returns objects to the pool.
    /// </summary>
    /// <remarks>
    /// Search strategy is a simple linear probing which is chosen for it cache-friendliness.
    /// Note that Free will try to store recycled objects close to the start thus statistically 
    /// reducing how far we will typically search in Allocate.
    /// </remarks>
    internal void Free(T obj)
    {
        Validate(obj);
        ForgetTrackedObject(obj);

        _clear?.Invoke(obj);

        if (_firstItem == null)
        {
            // Intentionally not using interlocked here. 
            // In a worst case scenario two objects may be stored into same slot.
            // It is very unlikely to happen and will only mean that one of the objects will get collected.
            _firstItem = obj;
        }
        else
        {
            FreeSlow(obj);
        }
    }

    /// <summary>
    /// Removes an object from leak tracking.  
    /// 
    /// This is called when an object is returned to the pool.  It may also be explicitly 
    /// called if an object allocated from the pool is intentionally not being returned
    /// to the pool.  This can be of use with pooled arrays if the consumer wants to 
    /// return a larger array to the pool than was originally allocated.
    /// </summary>
    [Conditional("DEBUG")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Uses static members when DETECT_LEAKS is set.")]
    internal void ForgetTrackedObject(T old, T? replacement = null)
    {
#if DETECT_LEAKS
        LeakTracker tracker;
        if (leakTrackers.TryGetValue(old, out tracker))
        {
            tracker.Dispose();
            leakTrackers.Remove(old);
        }
        else
        {
            var trace = CaptureStackTrace();
            Debug.WriteLine($"TRACEOBJECTPOOLLEAKS_BEGIN\nObject of type {typeof(T)} was freed, but was not from pool. \n Callstack: \n {trace} TRACEOBJECTPOOLLEAKS_END");
        }

        if (replacement != null)
        {
            tracker = new LeakTracker();
            leakTrackers.Add(replacement, tracker);
        }
#endif
    }

    private T CreateInstance()
    {
        var inst = _factory();
        return inst;
    }

    private T AllocateSlow()
    {
        var items = _items;

        for (var i = 0; i < items.Length; i++)
        {
            // Note that the initial read is optimistically not synchronized. That is intentional. 
            // We will interlock only when we have a candidate. in a worst case we may miss some
            // recently returned objects. Not a big deal.
            var inst = items[i].Value;
            if (inst != null)
            {
                if (inst == Interlocked.CompareExchange(ref items[i].Value, null, inst))
                {
                    return inst;
                }
            }
        }

        return CreateInstance();
    }

    private void FreeSlow(T obj)
    {
        var items = _items;
        for (var i = 0; i < items.Length; i++)
        {
            if (items[i].Value == null)
            {
                // Intentionally not using interlocked here. 
                // In a worst case scenario two objects may be stored into same slot.
                // It is very unlikely to happen and will only mean that one of the objects will get collected.
                items[i].Value = obj;
                break;
            }
        }
    }

#if DETECT_LEAKS
    private static Lazy<Type> _stackTraceType = new Lazy<Type>(() => Type.GetType("System.Diagnostics.StackTrace"));

    private static object CaptureStackTrace()
    {
        return Activator.CreateInstance(_stackTraceType.Value);
    }
#endif

    [Conditional("DEBUG")]
    private void Validate(object obj)
    {
        Debug.Assert(obj != null, "freeing null?");

        Debug.Assert(_firstItem != obj, "freeing twice?");

        var items = _items;
        for (var i = 0; i < items.Length; i++)
        {
            var value = items[i].Value;
            if (value == null)
            {
                return;
            }

            Debug.Assert(value != obj, "freeing twice?");
        }
    }

#if DETECT_LEAKS
    private class LeakTracker : IDisposable
    {
        private volatile bool disposed;

#if TRACE_LEAKS
        internal volatile object Trace = null;
#endif

        public void Dispose()
        {
            disposed = true;
            GC.SuppressFinalize(this);
        }

        private string GetTrace()
        {
#if TRACE_LEAKS
            return Trace == null ? "" : Trace.ToString();
#else
            return "Leak tracing information is disabled. Define TRACE_LEAKS on ObjectPool`1.cs to get more info \n";
#endif
        }

        ~LeakTracker()
        {
            if (!this.disposed && !Environment.HasShutdownStarted)
            {
                var trace = GetTrace();

                // If you are seeing this message it means that object has been allocated from the pool 
                // and has not been returned back. This is not critical, but turns pool into rather 
                // inefficient kind of "new".
                Debug.WriteLine($"TRACEOBJECTPOOLLEAKS_BEGIN\nPool detected potential leaking of {typeof(T)}. \n Location of the leak: \n {GetTrace()} TRACEOBJECTPOOLLEAKS_END");
            }
        }
    }
#endif

    [DebuggerDisplay("{Value,nq}")]
    private struct Element
    {
        internal T? Value;
    }
}