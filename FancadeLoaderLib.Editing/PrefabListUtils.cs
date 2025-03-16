// <copyright file="PrefabListUtils.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Partial;
using System.Collections.Generic;
using System.Linq;

namespace FancadeLoaderLib.Editing;

/// <summary>
/// Utils for working with <see cref="PrefabList"/>.
/// </summary>
public static class PrefabListUtils
{
	/// <summary>
	/// Gets all of the levels from the prefab list.
	/// </summary>
	/// <remarks>
	/// <see cref="PrefabSegment.Type"/> == <see cref="PrefabType.Level"/>.
	/// </remarks>
	/// <param name="list">The list to get the levels from.</param>
	/// <returns><see cref="IEnumerable{T}"/> iterating over the levels in <paramref name="list"/>.</returns>
	public static IEnumerable<Prefab> GetLevels(this PrefabList list)
		=> list.Prefabs.Where(group => group.Type == PrefabType.Level);

	/// <summary>
	/// Gets all of the levels from the prefab list.
	/// </summary>
	/// <remarks>
	/// <see cref="PartialPrefabSegment.Type"/> == <see cref="PrefabType.Level"/>.
	/// </remarks>
	/// <param name="list">The list to get the levels from.</param>
	/// <returns><see cref="IEnumerable{T}"/> iterating over the levels in <paramref name="list"/>.</returns>
	public static IEnumerable<PartialPrefab> GetLevels(this PartialPrefabList list)
		=> list.Prefabs.Where(group => group.Type == PrefabType.Level);

	/// <summary>
	/// Gets all of the blocks from the prefab list.
	/// </summary>
	/// <remarks>
	/// <see cref="Prefab.Type"/> != <see cref="PrefabType.Level"/>.
	/// </remarks>
	/// <param name="list">The list to get the blocks from.</param>
	/// <returns><see cref="IEnumerable{T}"/> iterating over the blocks in <paramref name="list"/>.</returns>
	public static IEnumerable<Prefab> GetBlocks(this PrefabList list)
		=> list.Prefabs.Where(group => group.Type != PrefabType.Level);

	/// <summary>
	/// Gets all of the blocks from the prefab list.
	/// </summary>
	/// <remarks>
	/// <see cref="PartialPrefab.Type"/> != <see cref="PrefabType.Level"/>.
	/// </remarks>
	/// <param name="list">The list to get the blocks from.</param>
	/// <returns><see cref="IEnumerable{T}"/> iterating over the blocks in <paramref name="list"/>.</returns>
	public static IEnumerable<PartialPrefab> GetBlocks(this PartialPrefabList list)
		=> list.Prefabs.Where(group => group.Type != PrefabType.Level);
}
