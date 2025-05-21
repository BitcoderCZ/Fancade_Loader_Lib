// <copyright file="TerminalConnector.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Editing.Scripting.TerminalStores;
using System;

namespace FancadeLoaderLib.Editing.Scripting.Utils;

/// <summary>
/// A helper class for connecting <see cref="ITerminalStore"/>s together.
/// </summary>
public sealed class TerminalConnector
{
    private readonly Action<ITerminalStore, ITerminalStore> _connectFunc;

    private ITerminalStore? _firstStore;
    private ITerminalStore? _lastStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalConnector"/> class.
    /// </summary>
    /// <param name="connectFunc">A method that connects 2 <see cref="ITerminalStore"/>s together.</param>
    public TerminalConnector(Action<ITerminalStore, ITerminalStore> connectFunc)
    {
        _connectFunc = connectFunc;
    }

    /// <summary>
    /// Gets a <see cref="ITerminalStore"/> whose input and output are the first and last <see cref="ITerminalStore"/> added.
    /// </summary>
    /// <value>A <see cref="ITerminalStore"/> whose input and output are the first and last <see cref="ITerminalStore"/> added.</value>
    public ITerminalStore Store => _firstStore is not null && _lastStore is not null ? new MultiTerminalStore(_firstStore, _lastStore) : NopTerminalStore.Instance;

    /// <summary>
    /// Adds a <see cref="ITerminalStore"/> and connects it to the last one added.
    /// </summary>
    /// <param name="store">The <see cref="ITerminalStore"/> to add.</param>
    public void Add(ITerminalStore store)
    {
        if (store is NopTerminalStore)
        {
            return;
        }

        if (_lastStore is not null)
        {
            _connectFunc(_lastStore, store);
        }

        _firstStore ??= store;
        _lastStore = store;
    }

    /// <summary>
    /// Sets the last <see cref="ITerminalStore"/> without connecting it to the last one.
    /// </summary>
    /// <param name="store">The <see cref="ITerminalStore"/> to set as the last one.</param>
    public void SetLast(ITerminalStore store)
    {
        _firstStore ??= store;
        _lastStore = store;
    }

    /// <summary>
    /// Clears this <see cref="TerminalConnector"/>.
    /// </summary>
    public void Clear()
    {
        _firstStore = null;
        _lastStore = null;
    }
}
