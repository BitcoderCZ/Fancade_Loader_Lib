// <copyright file="ICodePlacer.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FancadeLoaderLib.Editing.Scripting;

public interface ICodePlacer
{
	public Block PlaceBlock(BlockDef blockType);
}
