// <copyright file="Disposable.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System;
using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib.Editing.Utils;

internal class Disposable : IDisposable
{
    private Action? _onDispose;

    public Disposable(Action onDispose)
    {
        ThrowIfNull(onDispose, nameof(onDispose));

        _onDispose = onDispose;
    }

    public void Dispose()
    {
        _onDispose?.Invoke();
        _onDispose = null;
    }
}
