// <copyright file="NopTerminalStore.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Editing.Scripting.Terminals;
using System;

namespace FancadeLoaderLib.Editing.Scripting.TerminalStores;

public sealed class NopTerminalStore : ITerminalStore
{
    public static readonly NopTerminalStore Instance = new NopTerminalStore();

    private NopTerminalStore()
    {
    }

    public ITerminal In => NopTerminal.Instance;

    public ReadOnlySpan<ITerminal> Out => [];
}
