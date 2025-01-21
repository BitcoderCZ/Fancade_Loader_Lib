// <copyright file="ICodePlacer.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FancadeLoaderLib.Editing.Scripting.Placers;

public interface ICodePlacer
{
	public Block PlaceBlock(BlockDef blockType);
}
