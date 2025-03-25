// <copyright file="ICodePlacer.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Editing.Scripting.Terminals;
using FancadeLoaderLib.Editing.Scripting.TerminalStores;
using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib.Editing.Scripting;

public interface ICodePlacer
{
    int PlacedBlockCount { get; }

    Block PlaceBlock(BlockDef blockType);

    void Connect(ITerminal fromTerminal, ITerminal toTerminal);

    void SetSetting(Block block, int settingIndex, object value);
}

public static class ICodePlacerUtils
{
    public static void Connect(this ICodePlacer placer, ITerminalStore fromStore, ITerminalStore toStore)
    {
        ThrowIfNull(placer, nameof(placer));

        if (fromStore is NopTerminalStore or null || toStore is NopTerminalStore or null)
        {
            return;
        }

        foreach (var target in fromStore.Out)
        {
            placer.Connect(target, toStore.In);
        }
    }

    public static void Connect(this ICodePlacer placer, ITerminalStore fromStore, ITerminal toTerminal)
    {
        ThrowIfNull(placer, nameof(placer));

        if (fromStore is NopTerminalStore or null || toTerminal is NopTerminal)
        {
            return;
        }

        foreach (var target in fromStore.Out)
        {
            placer.Connect(target, toTerminal);
        }
    }

    public static void Connect(this ICodePlacer placer, ITerminal fromTerminal, ITerminalStore toStore)
    {
        ThrowIfNull(placer, nameof(placer));

        if (fromTerminal is NopTerminal || toStore is NopTerminalStore or null)
        {
            return;
        }

        placer.Connect(fromTerminal, toStore.In);
    }
}