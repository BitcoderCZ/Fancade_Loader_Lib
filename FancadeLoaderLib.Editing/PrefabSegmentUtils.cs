// <copyright file="PrefabSegmentUtils.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib.Editing;

/// <summary>
/// Utils for working with <see cref="PrefabSegment"/>.
/// </summary>
public static class PrefabSegmentUtils
{
    /// <summary>
    /// Gets if a <see cref="PrefabSegment"/> is empty.
    /// </summary>
    /// <param name="segment">The prefab to test.</param>
    /// <returns><see langword="true"/> if <see cref="PrefabSegment.Voxels"/> is null or <see cref="Voxel.IsEmpty"/> is true for all of the voxels; otherwise, <see langword="false"/>.</returns>
    public static bool IsEmpty(this PrefabSegment segment)
    {
        ThrowIfNull(segment, nameof(segment));

        if (segment.Voxels is null)
        {
            return true;
        }

        for (int i = 0; i < segment.Voxels.Length; i++)
        {
            if (!segment.Voxels[i].IsEmpty)
            {
                return false;
            }
        }

        return true;
    }
}
