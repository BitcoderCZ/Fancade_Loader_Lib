using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using static FancadeLoaderLib.Utils.ThrowHelper;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System.Runtime.InteropServices;
#pragma warning restore IDE0130 // Namespace does not match folder structure

#if !NET5_0_OR_GREATER
internal static class CollectionsMarshal
{
    /// <summary>
    /// Get a <see cref="Span{T}"/> view over a <see cref="List{T}"/>'s data.
    /// Items should not be added or removed from the <see cref="List{T}"/> while the <see cref="Span{T}"/> is in use.
    /// </summary>
    /// <param name="list">The list to get the data view over.</param>
    /// <typeparam name="T">The type of the elements in the list.</typeparam>
    public static Span<T> AsSpan<T>(List<T>? list)
    {
        if (list is null)
        {
            return default;
        }

        int size = list.Count;
        T[] items = (T[])typeof(List<T>).GetField("_items", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(list)!;
        Debug.Assert(items is not null, "Implementation depends on List<T> always having an array.");

        if ((uint)size > (uint)items.Length)
        {
            // List<T> was erroneously mutated concurrently with this call, leading to a count larger than its array.
            ThrowInvalidOperationException();
        }

        var span = new Span<T>(items, 0, size);

        return span;
    }
}
#endif