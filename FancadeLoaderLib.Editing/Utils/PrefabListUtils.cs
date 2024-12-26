using FancadeLoaderLib.Partial;
using System.Collections.Generic;
using System.Linq;

namespace FancadeLoaderLib.Editing.Utils
{
	public static class PrefabListUtils
	{
		public static IEnumerable<Prefab> GetLevels(this PrefabList list)
			=> list.Where(prefab => prefab.Type == PrefabType.Level);
		public static IEnumerable<PartialPrefab> GetLevels(this PartialPrefabList list)
			=> list.Where(prefab => prefab.Type == PrefabType.Level);

		public static IEnumerable<Prefab> GetBlocks(this PrefabList list)
			=> list.Where(prefab => prefab.Type != PrefabType.Level);
		public static IEnumerable<PartialPrefab> GetBlocks(this PartialPrefabList list)
			=> list.Where(prefab => prefab.Type != PrefabType.Level);

		public static IEnumerable<IGrouping<ushort, Prefab>> GetGroups(this PrefabList list)
			=> list
				.Where(prefab => prefab.IsInGroup)
				.GroupBy(prefab => prefab.GroupId);
		public static IEnumerable<IGrouping<ushort, PartialPrefab>> GetGroups(this PartialPrefabList list)
			=> list
				.Where(prefab => prefab.IsInGroup)
				.GroupBy(prefab => prefab.GroupId);
		public static IEnumerable<PrefabGroup> GetGroupsAsGroups(this PrefabList list)
			=> list
				.Where(prefab => prefab.IsInGroup)
				.GroupBy(prefab => prefab.GroupId)
				.Select(group => new PrefabGroup(group));
		public static IEnumerable<PartialPrefabGroup> GetGroupsAsGroups(this PartialPrefabList list)
			=> list
				.Where(prefab => prefab.IsInGroup)
				.GroupBy(prefab => prefab.GroupId)
				.Select(group => new PartialPrefabGroup(group));

		public static IEnumerable<Prefab> GetGroup(this PrefabList list, ushort groupId)
			=> list.Where(prefab => prefab.GroupId == groupId);
		public static IEnumerable<PartialPrefab> GetGroup(this PartialPrefabList list, ushort groupId)
			=> list.Where(prefab => prefab.GroupId == groupId);
		public static PrefabGroup GetGroupAsGroup(this PrefabList list, ushort groupId)
			=> new PrefabGroup(list.Where(prefab => prefab.GroupId == groupId));
		public static PartialPrefabGroup GetGroupAsGroup(this PartialPrefabList list, ushort groupId)
			=> new PartialPrefabGroup(list.Where(prefab => prefab.GroupId == groupId));
	}
}
