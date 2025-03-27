// <copyright file="ICodePlacer.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Editing.Scripting.Terminals;
using FancadeLoaderLib.Editing.Scripting.TerminalStores;
using System.Collections;
using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib.Editing.Scripting;

/// <summary>
/// Exposes funcitons for placingand connecting blocks and setting setttings.
/// </summary>
public interface ICodePlacer
{
    /// <summary>
    /// Gets the number of blocks placed.
    /// </summary>
    /// <value>The number of blocks placed.</value>
    int PlacedBlockCount { get; }

    /// <summary>
    /// Places a blocks.
    /// </summary>
    /// <param name="blockType">The type of the block to place.</param>
    /// <returns>The placed block, it's position shouldn't be modified.</returns>
    Block PlaceBlock(BlockDef blockType);

    /// <summary>
    /// Connects 2 <see cref="ITerminal"/>s together.
    /// </summary>
    /// <param name="from">The first <see cref="ITerminal"/>.</param>
    /// <param name="to">The second <see cref="ITerminal"/>.</param>
    void Connect(ITerminal from, ITerminal to);

    /// <summary>
    /// Adds a setting to a block.
    /// </summary>
    /// <param name="block">The block to add the setting to.</param>
    /// <param name="settingIndex">Index of the setting.</param>
    /// <param name="value">The setting value.</param>
    void SetSetting(Block block, int settingIndex, object value);
}

/// <summary>
/// Utils for <see cref="ICodePlacer"/>.
/// </summary>
public static class ICodePlacerUtils
{
    /// <summary>
    /// Connects 2 <see cref="ITerminalStore"/>s together.
    /// </summary>
    /// <param name="placer">The <see cref="ICodePlacer"/> to use.</param>
    /// <param name="from">The first <see cref="ITerminalStore"/>.</param>
    /// <param name="to">The second <see cref="ITerminalStore"/>.</param>
    public static void Connect(this ICodePlacer placer, ITerminalStore from, ITerminalStore to)
    {
        ThrowIfNull(placer, nameof(placer));

        if (from is NopTerminalStore or null || to is NopTerminalStore or null)
        {
            return;
        }

        foreach (var target in from.Out)
        {
            placer.Connect(target, to.In);
        }
    }

    /// <summary>
    /// Connects a <see cref="ITerminalStore"/> to a <see cref="ITerminal"/>.
    /// </summary>
    /// <param name="placer">The <see cref="ICodePlacer"/> to use.</param>
    /// <param name="from">The first <see cref="ITerminalStore"/>.</param>
    /// <param name="to">The second <see cref="ITerminal"/>.</param>
    public static void Connect(this ICodePlacer placer, ITerminalStore from, ITerminal to)
    {
        ThrowIfNull(placer, nameof(placer));

        if (from is NopTerminalStore or null || to is NopTerminal)
        {
            return;
        }

        foreach (var target in from.Out)
        {
            placer.Connect(target, to);
        }
    }

    /// <summary>
    /// Connects a <see cref="ITerminal"/> to a <see cref="ITerminalStore"/>.
    /// </summary>
    /// <param name="placer">The <see cref="ICodePlacer"/> to use.</param>
    /// <param name="from">The first <see cref="ITerminal"/>.</param>
    /// <param name="to">The second <see cref="ITerminalStore"/>.</param>
    public static void Connect(this ICodePlacer placer, ITerminal from, ITerminalStore to)
    {
        ThrowIfNull(placer, nameof(placer));

        if (from is NopTerminal || to is NopTerminalStore or null)
        {
            return;
        }

        placer.Connect(from, to.In);
    }
}