// <copyright file="ITerminalStore.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Fancade.Editing.Scripting.Terminals;

namespace BitcoderCZ.Fancade.Editing.Scripting.TerminalStores;

/// <summary>
/// Represents an input <see cref="ITerminal"/> and multiple output <see cref="ITerminal"/>s.
/// </summary>
public interface ITerminalStore
{
    /// <summary>
    /// Gets the input terminal.
    /// </summary>
    /// <value>The input terminal.</value>
    ITerminal In { get; }

    /// <summary>
    /// Gets the output terminals.
    /// </summary>
    /// <value>The output terminals.</value>
    ReadOnlySpan<ITerminal> Out { get; }
}
