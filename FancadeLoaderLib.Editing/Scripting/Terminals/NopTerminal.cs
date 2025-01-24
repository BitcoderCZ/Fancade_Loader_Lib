// <copyright file="NopTerminal.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MathUtils.Vectors;

namespace FancadeLoaderLib.Editing.Scripting.Terminals;

public sealed class NopTerminal : ITerminal
{
	public static readonly NopTerminal Instance = new NopTerminal();

	private NopTerminal()
	{
	}

	public int3 BlockPosition => new int3(-1, -1, -1);

	public int TerminalIndex => -1;

	public int3? VoxelPosition => new int3(-1, -1, -1);

	public WireType WireType => WireType.Error;
}
