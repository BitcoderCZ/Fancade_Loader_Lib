// <copyright file="StockGroups.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Partial;
using MathUtils.Vectors;

namespace FancadeLoaderLib.Editing;

/// <summary>
/// Some of the built in blocks.
/// </summary>
public static class StockGroups
{
	/// <summary>
	/// The number block.
	/// </summary>
	public static readonly PartialPrefabGroup FloatLiteral = new PartialPrefabGroup(
	[
		new PartialPrefab("Number", PrefabType.Script, 36, new byte3(0, 0, 0)),
		new PartialPrefab("Number", PrefabType.Script, 36, new byte3(1, 0, 0)),
	],
	36);

	/// <summary>
	/// The vector block.
	/// </summary>
	public static readonly PartialPrefabGroup VectorLiteral = new PartialPrefabGroup(
	[
		new PartialPrefab("Vector", PrefabType.Script, 38, new byte3(0, 0, 0)),
		new PartialPrefab("Vector", PrefabType.Script, 38, new byte3(1, 0, 0)),
		new PartialPrefab("Vector", PrefabType.Script, 38, new byte3(0, 0, 1)),
		new PartialPrefab("Vector", PrefabType.Script, 38, new byte3(1, 0, 1)),
	],
	38);

	/// <summary>
	/// The rotation block.
	/// </summary>
	public static readonly PartialPrefabGroup RotationLiteral = new PartialPrefabGroup(
	[
		new PartialPrefab("Rotation", PrefabType.Script, 42, new byte3(0, 0, 0)),
		new PartialPrefab("Rotation", PrefabType.Script, 42, new byte3(1, 0, 0)),
		new PartialPrefab("Rotation", PrefabType.Script, 42, new byte3(0, 0, 1)),
		new PartialPrefab("Rotation", PrefabType.Script, 42, new byte3(1, 0, 1)),
	],
	42);

	/// <summary>
	/// The true block.
	/// </summary>
	public static readonly PartialPrefabGroup TrueLiteral = new PartialPrefabGroup(
	[
		new PartialPrefab("True", PrefabType.Script, 449, new byte3(0, 0, 0)),
		new PartialPrefab("True", PrefabType.Script, 449, new byte3(1, 0, 0)),
	],
	449);

	/// <summary>
	/// The false block.
	/// </summary>
	public static readonly PartialPrefabGroup FalseLiteral = new PartialPrefabGroup(
	[
		new PartialPrefab("False", PrefabType.Script, 451, new byte3(0, 0, 0)),
		new PartialPrefab("False", PrefabType.Script, 451, new byte3(1, 0, 0)),
	],
	451);
}
