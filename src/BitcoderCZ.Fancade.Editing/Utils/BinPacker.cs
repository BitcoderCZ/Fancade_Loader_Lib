// <copyright file="BinPacker.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Maths.Vectors;
using BitcoderCZ.Utils;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace BitcoderCZ.Fancade.Editing.Utils;

internal static class BinPacker
{
    public static int3[] Compute(int3[] sizes, int count)
    {
        ThrowHelper.ThrowIfNull(sizes);
        ThrowHelper.ThrowIfGreaterThanOrEqualToOrNegative(count, sizes.Length);

        if (count is 0)
        {
            return [];
        }
        else if (count is 1)
        {
            return [int3.Zero];
        }

        int3[] positions = new int3[count];

        int[] indices = ArrayPool<int>.Shared.Rent(count);
        long totalArea = 0;

        for (int i = 0; i < count; i++)
        {
            indices[i] = i;
            totalArea += (long)sizes[i].X * sizes[i].Z;
        }

        Array.Sort(indices, (a, b) => sizes[b].Z.CompareTo(sizes[a].Z));

        int maxWidth = (int)Math.Sqrt(totalArea);
        
        for (int i = 0; i < count; i++)
        {
            if (sizes[i].X > maxWidth)
            {
                maxWidth = sizes[i].X;
            }
        }

        int currentX = 0;
        int currentZ = 0;
        int shelfZLimit = 0;

        for (int i = 0; i < count; i++)
        {
            int idx = indices[i];
            int w = sizes[idx].X;
            int h = sizes[idx].Z;

            if (currentX + w > maxWidth)
            {
                currentX = 0;
                currentZ += shelfZLimit;
                shelfZLimit = 0;
            }

            positions[idx] = new int3(currentX, 0, currentZ);

            currentX += w;
            if (h > shelfZLimit)
            {
                shelfZLimit = h;
            }
        }

        ArrayPool<int>.Shared.Return(indices);

        return positions;
    }
}