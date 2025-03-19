// <copyright file="ITerminalStore.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Editing.Scripting.Terminals;
using System;

namespace FancadeLoaderLib.Editing.Scripting.TerminalStores;

public interface ITerminalStore
{
#pragma warning disable CA1716 // Identifiers should not match keywords
    ITerminal In { get; }
#pragma warning restore CA1716 // Identifiers should not match keywords

    ReadOnlySpan<ITerminal> Out { get; }
}
