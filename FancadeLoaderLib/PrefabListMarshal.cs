// <copyright file="PrefabListMarshal.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Partial;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FancadeLoaderLib;

/// <summary>
/// Provides a set of methods to access the underlying data representations of collections.
/// </summary>
public static class PrefabListMarshal
{
	/// <summary>
	/// Get a <see cref="Span{T}"/> view over a <see cref="PrefabList"/>'s data.
	/// Items should not be added or removed from the <see cref="PrefabList"/> while the <see cref="Span{T}"/> is in use.
	/// </summary>
	/// <param name="list">The list to get the data view over.</param>
	/// <returns>The underlying span of <paramref name="list"/>.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Span<Prefab> AsSpan(PrefabList list)
	{
		if (list is null)
		{
			throw new ArgumentNullException(nameof(list));
		}

#if NET5_0_OR_GREATER
		return CollectionsMarshal.AsSpan(list._list);
#else
		int size = list._list.Count;
		Prefab[] items = (Prefab[])typeof(List<Prefab>).GetField("_items", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(list._list);
		Debug.Assert(items is not null, "Implementation depends on List<T> always having an array.");

		if ((uint)size > (uint)items.Length)
		{
			// List<T> was erroneously mutated concurrently with this call, leading to a count larger than its array.
			throw new InvalidOperationException();
		}

		var span = new Span<Prefab>(items, 0, size);

		return span;
#endif
	}

	/// <summary>
	/// Get a <see cref="Span{T}"/> view over a <see cref="PartialPrefabList"/>'s data.
	/// Items should not be added or removed from the <see cref="PartialPrefabList"/> while the <see cref="Span{T}"/> is in use.
	/// </summary>
	/// <param name="list">The list to get the data view over.</param>
	/// <returns>The underlying span of <paramref name="list"/>.</returns>
	public static Span<PartialPrefab> AsSpan(PartialPrefabList list)
	{
		if (list is null)
		{
			throw new ArgumentNullException(nameof(list));
		}

#if NET5_0_OR_GREATER
		return CollectionsMarshal.AsSpan(list._list);
#else
		int size = list._list.Count;
		PartialPrefab[] items = (PartialPrefab[])typeof(List<PartialPrefab>).GetField("_items", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(list._list);
		Debug.Assert(items is not null, "Implementation depends on List<T> always having an array.");

		if ((uint)size > (uint)items.Length)
		{
			// List<T> was erroneously mutated concurrently with this call, leading to a count larger than its array.
			throw new InvalidOperationException();
		}

		var span = new Span<PartialPrefab>(items, 0, size);

		return span;
#endif
	}
}
