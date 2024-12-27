// <copyright file="PrefabCollider.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FancadeLoaderLib;

public enum PrefabCollider : byte
{
	/// <summary>
	/// The prefab doesn't have a collider.
	/// </summary>
	None = 0,

	/// <summary>
	/// Collider of the prefabs matches it's voxels.
	/// </summary>
	Box = 1,

	/// <summary>
	/// Collider of the prefab is a sphere.
	/// </summary>
	Sphere = 2,
}
