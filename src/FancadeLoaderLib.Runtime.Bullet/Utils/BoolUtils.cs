using System.Runtime.CompilerServices;

namespace FancadeLoaderLib.Runtime.Bullet.Utils;

internal static class BoolUtils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ToInt(this bool value)
        => value ? 1 : 0;
}
