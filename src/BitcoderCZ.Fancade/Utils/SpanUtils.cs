using System;
using System.Collections.Generic;
using static BitcoderCZ.Fancade.Utils.ThrowHelper;

namespace BitcoderCZ.Fancade.Utils;

internal static class SpanUtils
{
#if NETSTANDARD2_1
    public static void Sort<T, TComparer>(this Span<T> span, TComparer comparer)
       where TComparer : IComparer<T>
    {
        ThrowIfNull(comparer, nameof(comparer));

        if (span.Length > 1)
        {
            QuickSort(span, 0, span.Length - 1, comparer.Compare);
        }
    }

    public static void Sort<T>(this Span<T> span, Comparison<T> comparison)
    {
        ThrowIfNull(comparison, nameof(comparison));

        if (span.Length > 1)
        {
            QuickSort(span, 0, span.Length - 1, comparison);
        }
    }

    private static void QuickSort<T>(Span<T> span, int left, int right, Comparison<T> comparison)
    {
        while (left < right)
        {
            int pivotIndex = Partition(span, left, right, comparison);

            if (pivotIndex - left < right - pivotIndex)
            {
                QuickSort(span, left, pivotIndex - 1, comparison);
                left = pivotIndex + 1;
            }
            else
            {
                QuickSort(span, pivotIndex + 1, right, comparison);
                right = pivotIndex - 1;
            }
        }
    }

    private static int Partition<T>(Span<T> span, int left, int right, Comparison<T> comparison)
    {
        T pivot = span[right];
        int i = left - 1;

        for (int j = left; j < right; j++)
        {
            if (comparison(span[j], pivot) <= 0)
            {
                i++;
                Swap(ref span[i], ref span[j]);
            }
        }

        Swap(ref span[i + 1], ref span[right]);
        return i + 1;
    }

    private static void Swap<T>(ref T a, ref T b)
    {
        T tmp = a;
        a = b;
        b = tmp;
    }
#endif
}
