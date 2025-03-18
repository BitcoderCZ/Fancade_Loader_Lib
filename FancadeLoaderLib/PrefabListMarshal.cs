// <copyright file="PrefabListMarshal.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Partial;
using FancadeLoaderLib.Utils;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static FancadeLoaderLib.Utils.ThrowHelper;

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
	public static Span<PrefabSegment> AsSegmentSpan(PrefabList list)
	{
		ThrowIfNull(list, nameof(list));

		return CollectionsMarshal.AsSpan(list._segments);
	}

	/// <summary>
	/// Get a <see cref="Span{T}"/> view over a <see cref="PartialPrefabList"/>'s data.
	/// Items should not be added or removed from the <see cref="PartialPrefabList"/> while the <see cref="Span{T}"/> is in use.
	/// </summary>
	/// <param name="list">The list to get the data view over.</param>
	/// <returns>The underlying span of <paramref name="list"/>.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Span<PartialPrefabSegment> AsSegmentSpan(PartialPrefabList list)
	{
		ThrowIfNull(list, nameof(list));

		return CollectionsMarshal.AsSpan(list._segments);
	}
}
