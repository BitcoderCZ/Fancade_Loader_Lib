// <copyright file="PrefabListUtils.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
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
	/// <see cref="Prefab.Type"/> == <see cref="PrefabType.Level"/>.
	/// </remarks>
	/// <param name="list">The list to get the levels from.</param>
	/// <returns><see cref="IEnumerable{T}"/> iterating over the levels in <paramref name="list"/>.</returns>
	public static IEnumerable<Prefab> GetLevels(this PrefabList list)
		=> list.Where(prefab => prefab.Type == PrefabType.Level);

	/// <summary>
	/// Gets all of the levels from the prefab list.
	/// </summary>
	/// <remarks>
	/// <see cref="PartialPrefab.Type"/> == <see cref="PrefabType.Level"/>.
	/// </remarks>
	/// <param name="list">The list to get the levels from.</param>
	/// <returns><see cref="IEnumerable{T}"/> iterating over the levels in <paramref name="list"/>.</returns>
	public static IEnumerable<PartialPrefab> GetLevels(this PartialPrefabList list)
		=> list.Where(prefab => prefab.Type == PrefabType.Level);

	/// <summary>
	/// Gets all of the blocks from the prefab list.
	/// </summary>
	/// <remarks>
	/// <see cref="Prefab.Type"/> != <see cref="PrefabType.Level"/>.
	/// </remarks>
	/// <param name="list">The list to get the blocks from.</param>
	/// <returns><see cref="IEnumerable{T}"/> iterating over the blocks in <paramref name="list"/>.</returns>
	public static IEnumerable<Prefab> GetBlocks(this PrefabList list)
		=> list.Where(prefab => prefab.Type != PrefabType.Level);

	/// <summary>
	/// Gets all of the blocks from the prefab list.
	/// </summary>
	/// <remarks>
	/// <see cref="PartialPrefab.Type"/> != <see cref="PrefabType.Level"/>.
	/// </remarks>
	/// <param name="list">The list to get the blocks from.</param>
	/// <returns><see cref="IEnumerable{T}"/> iterating over the blocks in <paramref name="list"/>.</returns>
	public static IEnumerable<PartialPrefab> GetBlocks(this PartialPrefabList list)
		=> list.Where(prefab => prefab.Type != PrefabType.Level);

	/// <summary>
	/// Gets all of the groups from a prefab list.
	/// </summary>
	/// <param name="list">The list to get the groups from.</param>
	/// <returns><see cref="IEnumerable{T}"/> iterating over the groups in <paramref name="list"/>.</returns>
	public static IEnumerable<IGrouping<ushort, Prefab>> GetGroupsEnumerable(this PrefabList list)
		=> list
			.Where(prefab => prefab.IsInGroup)
			.GroupBy(prefab => prefab.GroupId);

	/// <summary>
	/// Gets all of the groups from a prefab list.
	/// </summary>
	/// <param name="list">The list to get the groups from.</param>
	/// <returns><see cref="IEnumerable{T}"/> iterating over the groups in <paramref name="list"/>.</returns>
	public static IEnumerable<IGrouping<ushort, PartialPrefab>> GetGroupsEnumerable(this PartialPrefabList list)
		=> list
			.Where(prefab => prefab.IsInGroup)
			.GroupBy(prefab => prefab.GroupId);

	/// <summary>
	/// Gets all of the groups from a prefab list, converting them to <see cref="PrefabGroup"/>.
	/// </summary>
	/// <param name="list">The list to get the groups from.</param>
	/// <returns><see cref="IEnumerable{T}"/> iterating over the groups in <paramref name="list"/>.</returns>
	public static IEnumerable<PrefabGroup> GetGroups(this PrefabList list)
		=> list
			.Where(prefab => prefab.IsInGroup)
			.GroupBy(prefab => prefab.GroupId)
			.Select(group => new PrefabGroup(group));

	/// <summary>
	/// Gets all of the groups from a prefab list, converting them to <see cref="PartialPrefabGroup"/>.
	/// </summary>
	/// <param name="list">The list to get the groups from.</param>
	/// <returns><see cref="IEnumerable{T}"/> iterating over the groups in <paramref name="list"/>.</returns>
	public static IEnumerable<PartialPrefabGroup> GetGroups(this PartialPrefabList list)
		=> list
			.Where(prefab => prefab.IsInGroup)
			.GroupBy(prefab => prefab.GroupId)
			.Select(group => new PartialPrefabGroup(group));

	/// <summary>
	/// Gets a group with the specified id from a prefab list.
	/// </summary>
	/// <param name="list">The list to get the group from.</param>
	/// <param name="groupId">Id of the group.</param>
	/// <returns><see cref="IEnumerable{T}"/> iterating over the prefabs in the group.</returns>
	public static IEnumerable<Prefab> GetGroupEnumerable(this PrefabList list, ushort groupId)
		=> list.Where(prefab => prefab.GroupId == groupId);

	/// <summary>
	/// Gets a group with the specified id from a prefab list.
	/// </summary>
	/// <param name="list">The list to get the group from.</param>
	/// <param name="groupId">Id of the group.</param>
	/// <returns><see cref="IEnumerable{T}"/> iterating over the prefabs in the group.</returns>
	public static IEnumerable<PartialPrefab> GetGroupEnumerable(this PartialPrefabList list, ushort groupId)
		=> list.Where(prefab => prefab.GroupId == groupId);

	/// <summary>
	/// Gets a group with the specified id from a prefab list.
	/// </summary>
	/// <param name="list">The list to get the group from.</param>
	/// <param name="groupId">Id of the group.</param>
	/// <returns>The group with the specified id.</returns>
	public static PrefabGroup GetGroup(this PrefabList list, ushort groupId)
		=> new PrefabGroup(list.Where(prefab => prefab.GroupId == groupId));

	/// <summary>
	/// Gets a group with the specified id from a prefab list.
	/// </summary>
	/// <param name="list">The list to get the group from.</param>
	/// <param name="groupId">Id of the group.</param>
	/// <returns>The group with the specified id.</returns>
	public static PartialPrefabGroup GetGroup(this PartialPrefabList list, ushort groupId)
		=> new PartialPrefabGroup(list.Where(prefab => prefab.GroupId == groupId));
}
