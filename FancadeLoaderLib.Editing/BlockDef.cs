// <copyright file="BlockDef.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Editing.Utils;
using FancadeLoaderLib.Partial;
using FancadeLoaderLib.Utils;
using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace FancadeLoaderLib.Editing;

public sealed class BlockDef
{
	public readonly PartialPrefabGroup Prefab;

	public readonly BlockType BlockType;

	public readonly ImmutableArray<TerminalDef> Terminals;

	public BlockDef(PartialPrefabGroup prefab, BlockType blockType, TerminalBuilder terminals)
	{
		Prefab = prefab;
		BlockType = blockType;
		Terminals = terminals.Build(Prefab.Size, BlockType);
	}

	public BlockDef(string name, ushort id, BlockType blockType, PrefabType prefabType, int3 size, TerminalBuilder terminals)
	{
		if (size.X < 0 || size.Y < 1 || size.Z < 1)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(nameof(size), $"{nameof(size)} cannot be negative or zero.");
		}

		Prefab = new PartialPrefabGroup(id, name, prefabType, [byte3.Zero]);
		BlockType = blockType;
		Terminals = terminals.Build(Prefab.Size, BlockType);

		for (int z = 0; z < size.Z; z++)
		{
			for (int y = 0; y < size.Y; y++)
			{
				for (int x = 0; x < size.X; x++)
				{
					Prefab.Add(new byte3(x, y, z));
				}
			}
		}
	}

	public TerminalDef Before => BlockType == BlockType.Active ? Terminals.Get(^1) : throw new InvalidOperationException("Only active blocks have Before and After");

	public TerminalDef After => BlockType == BlockType.Active ? Terminals[0] : throw new InvalidOperationException("Only active blocks have Before and After");

	public int3 Size => Prefab.Size;

	public bool IsGroup => Size != int3.One;

	public TerminalDef this[string terminalName]
	{
		get
		{
			foreach (var terminal in Terminals)
			{
				if (terminal.Name == terminalName)
				{
					return terminal;
				}
			}

			ThrowKeyNotFoundException($"This block doesn't contain a terminal with the name '{terminalName}'.");
			return null!;
		}
	}

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void ThrowKeyNotFoundException(string paramName)
		=> throw new KeyNotFoundException(paramName);
}
