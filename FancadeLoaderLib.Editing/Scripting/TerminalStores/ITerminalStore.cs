// <copyright file="ITerminalStore.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Editing.Scripting.Terminals;
using System;

namespace FancadeLoaderLib.Editing.Scripting.TerminalStores;

public interface ITerminalStore
{
	ITerminal In { get; }

	ReadOnlySpan<ITerminal> Out { get; }
}
