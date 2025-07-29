// <copyright file="MultiTerminalStore.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Fancade.Editing.Scripting.Terminals;
using System;

namespace BitcoderCZ.Fancade.Editing.Scripting.TerminalStores;

/// <summary>
/// A <see cref="ITerminalStore"/> that is based on other <see cref="ITerminalStore"/>s.
/// </summary>
public sealed class MultiTerminalStore : ITerminalStore
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MultiTerminalStore"/> class.
    /// </summary>
    /// <param name="inStore">The input <see cref="ITerminalStore"/>.</param>
    /// <param name="outStore">The output <see cref="ITerminalStore"/>.</param>
    public MultiTerminalStore(ITerminalStore inStore, ITerminalStore outStore)
    {
        InStore = inStore;
        OutStore = outStore;
    }

    /// <summary>
    /// Gets the input <see cref="ITerminalStore"/>.
    /// </summary>
    /// <value>The input <see cref="ITerminalStore"/>.</value>
    public ITerminalStore InStore { get; }

    /// <summary>
    /// Gets the output <see cref="ITerminalStore"/>.
    /// </summary>
    /// <value>The output <see cref="ITerminalStore"/>.</value>
    public ITerminalStore OutStore { get; }

    /// <inheritdoc/>
    public ITerminal In => InStore.In;

    /// <inheritdoc/>
    public ReadOnlySpan<ITerminal> Out => OutStore.Out;
}
