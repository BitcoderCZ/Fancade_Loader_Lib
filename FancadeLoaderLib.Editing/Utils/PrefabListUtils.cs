using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FancadeLoaderLib.Editing.Utils
{
    public static class PrefabListUtils
    {
        public static IEnumerable<Prefab> GetLevels(this PrefabList list)
            => list.Where(prefab => prefab.Type == PrefabType.Level);

        public static IEnumerable<Prefab> GetBlocks(this PrefabList list)
            => list.Where(prefab => prefab.Type != PrefabType.Level);

        public static IEnumerable<IGrouping<ushort, Prefab>> GetGroups(this PrefabList list)
            => list
                .Where(prefab => prefab.IsInGourp)
                .GroupBy(prefab => prefab.GroupId);
        public static IEnumerable<PrefabGroup> GetGroupsAsGroups(this PrefabList list)
            => list
                .Where(prefab => prefab.IsInGourp)
                .GroupBy(prefab => prefab.GroupId)
                .Select(group => new PrefabGroup(group));

        public static IEnumerable<Prefab> GetGroup(this PrefabList list, ushort groupId)
            => list.Where(prefab => prefab.GroupId == groupId);
        public static PrefabGroup GetGroupAsGroup(this PrefabList list, ushort groupId)
            => new PrefabGroup(list.Where(prefab => prefab.GroupId == groupId));
    }
}
