// <copyright file="NopTerminalStore.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Fancade.Editing.Scripting.Terminals;

namespace BitcoderCZ.Fancade.Editing.Scripting.TerminalStores;

/// <summary>
/// A <see cref="ITerminalStore"/> that doesn't connect to anything.
/// </summary>
public sealed class NopTerminalStore : ITerminalStore
{
    /// <summary>
    /// The <see cref="NopTerminalStore"/> instance.
    /// </summary>
    public static readonly NopTerminalStore Instance = new NopTerminalStore();

    private NopTerminalStore()
    {
    }

    /// <inheritdoc/>
    public ITerminal In => NopTerminal.Instance;

    /// <inheritdoc/>
    public ReadOnlySpan<ITerminal> Out => [];
}
