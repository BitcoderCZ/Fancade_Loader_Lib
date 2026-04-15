// <copyright file="TerminalConnector.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using TerminalStore = BitcoderCZ.Fancade.Editing.Scripting.Node.TerminalStore;

namespace BitcoderCZ.Fancade.Editing.Scripting;

/// <summary>
/// A helper class for connecting <see cref="TerminalStore"/>s together.
/// </summary>
public sealed class TerminalConnector
{
    private readonly Action<TerminalStore, TerminalStore> _connect;

    // todo: could just be Terminal
    private TerminalStore? _firstStore;
    private TerminalStore? _lastStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalConnector"/> class.
    /// </summary>
    /// <param name="builder">The <see cref="CodeGraph.Builder"/> used to connect terminals.</param>
    public TerminalConnector(CodeGraph.IBuilder builder)
        : this(builder.Connect)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalConnector"/> class.
    /// </summary>
    /// <param name="connectAction">A delegate that connects 2 <see cref="TerminalStore"/>s together.</param>
    public TerminalConnector(Action<TerminalStore, TerminalStore> connectAction)
    {
        _connect = connectAction;
    }

    /// <summary>
    /// Gets an <see cref="TerminalStore"/> whose input and output are the first and last <see cref="TerminalStore"/> added.
    /// </summary>
    /// <value>A <see cref="TerminalStore"/> whose input and output are the first and last <see cref="TerminalStore"/> added.</value>
    public TerminalStore Store => _firstStore is null || _lastStore is null ? default : TerminalStore.Combine(_firstStore.Value, _lastStore.Value);

    /// <summary>
    /// Adds a <see cref="TerminalStore"/> and connects it to the last one added.
    /// </summary>
    /// <param name="store">The <see cref="TerminalStore"/> to add.</param>
    public void Add(TerminalStore store)
    {
        if (_lastStore is { } lastStore)
        {
            _connect(lastStore, store);
        }

        _firstStore ??= store;
        _lastStore = store;
    }

    /// <summary>
    /// Sets the last <see cref="TerminalStore"/> without connecting it to the last one.
    /// </summary>
    /// <param name="store">The <see cref="TerminalStore"/> to set as the last one.</param>
    public void SetLast(TerminalStore store)
    {
        _firstStore ??= store;
        _lastStore = store;
    }

    /// <summary>
    /// Clears this <see cref="TerminalConnector"/>.
    /// </summary>
    public void Clear()
    {
        _firstStore = default;
        _lastStore = default;
    }
}
