// <copyright file="Disposable.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System;

namespace FancadeLoaderLib.Editing.Utils;

internal class Disposable : IDisposable
{
	private Action? _onDispose;

	public Disposable(Action onDispose)
	{
		if (onDispose is null)
		{
			throw new ArgumentNullException(nameof(onDispose));
		}

		_onDispose = onDispose;
	}

	public void Dispose()
	{
		_onDispose?.Invoke();
		_onDispose = null;
	}
}
