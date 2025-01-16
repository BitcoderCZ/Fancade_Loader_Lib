// <copyright file="PrefabUtils.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FancadeLoaderLib.Editing;

/// <summary>
/// Utils for working with <see cref="Prefab"/>.
/// </summary>
public static class PrefabUtils
{
	/// <summary>
	/// Gets if a <see cref="Prefab"/> is empty.
	/// </summary>
	/// <param name="prefab">The prefab to test.</param>
	/// <returns><see langword="true"/> if <see cref="Prefab.Voxels"/> is null or <see cref="Voxel.IsEmpty"/> is true for all of the voxels; otherwise, <see langword="false"/>.</returns>
	public static bool IsEmpty(this Prefab prefab)
	{
		if (prefab.Voxels is null)
		{
			return true;
		}

		for (int i = 0; i < prefab.Voxels.Length; i++)
		{
			if (!prefab.Voxels[i].IsEmpty)
			{
				return false;
			}
		}

		return true;
	}
}
