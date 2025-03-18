// <copyright file="PartialPrefabSegment.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MathUtils.Vectors;
using System;

namespace FancadeLoaderLib.Partial;

/// <summary>
/// <see cref="PrefabSegment"/>, but for <see cref="PartialPrefab"/>.
/// </summary>
public class PartialPrefabSegment : ICloneable
{
	/// <summary>
	/// Initializes a new instance of the <see cref="PartialPrefabSegment"/> class.
	/// </summary>
	/// <param name="prefabId">Id of the prefab this segment is in.</param>
	/// <param name="posInPrefab">Position of this segment in prefab.</param>
	public PartialPrefabSegment(ushort prefabId, byte3 posInPrefab)
	{
		PrefabId = prefabId;
		PosInPrefab = posInPrefab;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PartialPrefabSegment"/> class.
	/// </summary>
	/// <param name="other">The segment to copy.</param>
	public PartialPrefabSegment(PartialPrefabSegment other)
	{
		PrefabId = other.PrefabId;
		PosInPrefab = other.PosInPrefab;
	}

	/// <summary>
	/// Gets the id of the prefab this segment is in.
	/// </summary>
	/// <value>Id of the prefab this segment is in.</value>
	public ushort PrefabId { get; internal set; }

	/// <summary>
	/// Gets the position of this segment in prefab.
	/// </summary>
	/// <value>Position of this segment in prefab.</value>
	public byte3 PosInPrefab { get; internal set; }

	/// <summary>
	/// Creates a copy of this <see cref="PartialPrefabSegment"/>.
	/// </summary>
	/// <returns>A copy of this <see cref="PartialPrefabSegment"/>.</returns>
	public PartialPrefabSegment Clone()
		=> new PartialPrefabSegment(this);

	/// <inheritdoc/>
	object ICloneable.Clone()
		=> new PartialPrefabSegment(this);
}
