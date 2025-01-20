// <copyright file="TerminalConnector.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Editing.Scripting.TerminalStores;
using System;

namespace FancadeLoaderLib.Editing.Scripting.Utils;

public sealed class TerminalConnector
{
	private readonly Action<ITerminalStore, ITerminalStore> _connectFunc;

	private ITerminalStore? _firstStore;
	private ITerminalStore? _lastStore;

	public TerminalConnector(Action<ITerminalStore, ITerminalStore> connectFunc)
	{
		_connectFunc = connectFunc;
	}

	public ITerminalStore Store => _firstStore is not null && _lastStore is not null ? new MultiTerminalStore(_firstStore, _lastStore) : NopTerminalStore.Instance;

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
}
