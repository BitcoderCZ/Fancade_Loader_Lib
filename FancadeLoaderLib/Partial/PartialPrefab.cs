using MathUtils.Vectors;
using System;

namespace FancadeLoaderLib.Partial;

public class PartialPrefab : ICloneable
{
	public PartialPrefab(ushort groupId, byte3 posInGroup)
	{
		GroupId = groupId;
		PosInGroup = posInGroup;
	}

	public PartialPrefab(PartialPrefab other)
	{
		GroupId = other.GroupId;
		PosInGroup = other.PosInGroup;
	}

	public ushort GroupId { get; internal set; }

	public byte3 PosInGroup { get; internal set; }

	/// <summary>
	/// Creates a copy of this <see cref="PartialPrefab"/>.
	/// </summary>
	/// <returns>A copy of this <see cref="PartialPrefab"/>.</returns>
	public PartialPrefab Clone()
		=> new PartialPrefab(this);

	/// <inheritdoc/>
	object ICloneable.Clone()
		=> new PartialPrefab(this);
}
