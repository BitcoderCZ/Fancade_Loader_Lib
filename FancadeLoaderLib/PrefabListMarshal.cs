// <copyright file="PrefabListMarshal.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Partial;
using System;
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
	public static Span<Prefab> AsSpan(PrefabList list)
		=> CollectionsMarshal.AsSpan(list._list);

	/// <summary>
	/// Get a <see cref="Span{T}"/> view over a <see cref="PartialPrefabList"/>'s data.
	/// Items should not be added or removed from the <see cref="PartialPrefabList"/> while the <see cref="Span{T}"/> is in use.
	/// </summary>
	/// <param name="list">The list to get the data view over.</param>
	/// <returns>The underlying span of <paramref name="list"/>.</returns>
	public static Span<PartialPrefab> AsSpan(PartialPrefabList list)
		=> CollectionsMarshal.AsSpan(list._list);
}
