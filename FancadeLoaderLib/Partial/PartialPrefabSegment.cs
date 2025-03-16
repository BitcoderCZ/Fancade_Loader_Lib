// <copyright file="PartialPrefabSegment.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MathUtils.Vectors;
using System;

namespace FancadeLoaderLib.Partial;

public class PartialPrefabSegment : ICloneable
{
	public PartialPrefabSegment(ushort prefabId, byte3 posInPrefab)
	{
		PrefabId = prefabId;
		PosInPrefab = posInPrefab;
	}

	public PartialPrefabSegment(PartialPrefabSegment other)
	{
		PrefabId = other.PrefabId;
		PosInPrefab = other.PosInPrefab;
	}

	public ushort PrefabId { get; internal set; }

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
