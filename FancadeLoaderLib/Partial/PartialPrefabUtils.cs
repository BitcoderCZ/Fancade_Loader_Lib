// <copyright file="PartialPrefabUtils.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

#pragma warning disable CA1716
namespace FancadeLoaderLib.Partial;
#pragma warning restore CA1716

/// <summary>
/// Util funcitons for <see cref="PartialPrefab"/>.
/// </summary>
public static class PartialPrefabUtils
{
	/// <summary>
	/// Coverts a <see cref="PrefabGroup"/> to <see cref="PartialPrefabGroup"/>.
	/// </summary>
	/// <param name="group">The group to convert.</param>
	/// <returns><paramref name="group"/> coverted to <see cref="PartialPrefabGroup"/>.</returns>
	public static PartialPrefabGroup ToPartial(this PrefabGroup group)
		=> new PartialPrefabGroup(group);
}
