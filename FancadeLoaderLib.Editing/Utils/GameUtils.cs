using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FancadeLoaderLib.Editing.Utils
{
    public static class GameUtils
    {
        public static IEnumerable<Prefab> GetLevels(this Game game)
            => game.Prefabs.Where(prefab => prefab.Type == PrefabType.Level);

        public static IEnumerable<Prefab> GetBlocks(this Game game)
            => game.Prefabs.Where(prefab => prefab.Type != PrefabType.Level);

        public static IEnumerable<IGrouping<ushort, Prefab>> GetGroups(this Game game)
            => game.Prefabs
                .Where(prefab => prefab.IsInGourp)
                .GroupBy(prefab => prefab.GroupId);

        public static IEnumerable<Prefab> GetGroup(this Game game, ushort groupId)
            => game.Prefabs.Where(prefab => prefab.GroupId == groupId);
    }
}
