// <copyright file="PartialPrefabUtils.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

#pragma warning disable CA1716
namespace BitcoderCZ.Fancade.Partial;
#pragma warning restore CA1716

/// <summary>
/// Util funcitons for <see cref="PartialPrefabSegment"/>.
/// </summary>
public static class PartialPrefabUtils
{
    /// <summary>
    /// Coverts a <see cref="Prefab"/> to <see cref="PartialPrefab"/>.
    /// </summary>
    /// <param name="prefab">The prefab to convert.</param>
    /// <returns><paramref name="prefab"/> coverted to <see cref="PartialPrefab"/>.</returns>
    public static PartialPrefab ToPartial(this Prefab prefab)
        => new PartialPrefab(prefab);

    /// <summary>
    /// Coverts a <see cref="PrefabList"/> to <see cref="PartialPrefabList"/>.
    /// </summary>
    /// <param name="list">The prefab list to convert.</param>
    /// <returns><paramref name="list"/> coverted to <see cref="PartialPrefabList"/>.</returns>
    public static PartialPrefabList ToPartial(this PrefabList list)
        => new PartialPrefabList(list);
}
