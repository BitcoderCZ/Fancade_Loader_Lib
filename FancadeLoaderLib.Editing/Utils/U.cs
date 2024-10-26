using MathUtils.Vectors;
using System;
using System.Threading.Tasks;

namespace FancadeLoaderLib.Editing.Utils
{
    public static class U
    {
        public static void Loop(Vector3I from, Vector3I to, Action<int, int, int> action)
            => Loop(from.X, from.Y, from.Z, to.X, to.Y, to.Z, action);
        public static void Loop(int fromX, int fromY, int fromZ, int toX, int toY, int toZ, Action<int, int, int> action)
        {
            for (int z = fromZ; z <= toZ; z++)
                for (int y = fromY; y <= toY; y++)
                    for (int x = fromX; x <= toX; x++)
                        action.Invoke(x, y, z);
        }
        public static void LoopParallel(Vector3I from, Vector3I to, Action<int, int, int> action)
            => LoopParallel(from.X, from.Y, from.Z, to.X, to.Y, to.Z, action);
        public static void LoopParallel(int fromX, int fromY, int fromZ, int toX, int toY, int toZ, Action<int, int, int> action)
        {
            Parallel.For(fromZ, toZ + 1, z =>
            {
                for (int y = fromY; y <= toY; y++)
                    for (int x = fromX; x <= toX; x++)
                        action.Invoke(x, y, z);
            });
        }
    }
}
