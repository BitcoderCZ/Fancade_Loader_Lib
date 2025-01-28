// <copyright file="PartialPrefabUtils.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Raw;

#pragma warning disable CA1716
namespace FancadeLoaderLib.Partial;
#pragma warning restore CA1716

/// <summary>
/// Util funcitons for <see cref="PartialPrefab"/>.
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
	/// Coverts a <see cref="RawPrefab"/> to <see cref="PartialPrefab"/>.
	/// </summary>
	/// <param name="prefab">The prefab to convert.</param>
	/// <returns><paramref name="prefab"/> coverted to <see cref="PartialPrefab"/>.</returns>
	public static PartialPrefab ToPartial(this RawPrefab prefab)
		=> new PartialPrefab(prefab);
}
