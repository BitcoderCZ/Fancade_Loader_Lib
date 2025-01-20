// <copyright file="MultiTerminalStore.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Editing.Scripting.Terminals;
using System;

namespace FancadeLoaderLib.Editing.Scripting.TerminalStores;

public sealed class MultiTerminalStore : ITerminalStore
{
	public MultiTerminalStore(ITerminalStore inStore, ITerminalStore outStore)
	{
		InStore = inStore;
		OutStore = outStore;
	}

	public ITerminalStore InStore { get; }

	public ITerminalStore OutStore { get; }

	public ITerminal In => InStore.In;

	public ReadOnlySpan<ITerminal> Out => OutStore.Out;
}
