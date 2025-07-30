// <copyright file="BlockDef.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Fancade.Editing.Utils;
using BitcoderCZ.Fancade.Partial;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using static BitcoderCZ.Fancade.Utils.ThrowHelper;

namespace BitcoderCZ.Fancade.Editing;

/// <summary>
/// Represents the type of a fancade block.
/// </summary>
public sealed class BlockDef
{
    /// <summary>
    /// The <see cref="PartialPrefab"/> of this block.
    /// </summary>
    public readonly PartialPrefab Prefab;

    /// <summary>
    /// The script type of this block.
    /// </summary>
    public readonly ScriptBlockType BlockType;

    /// <summary>
    /// The terminals of this block.
    /// </summary>
    public readonly ImmutableArray<TerminalDef> Terminals;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlockDef"/> class.
    /// </summary>
    /// <param name="prefab">The <see cref="PartialPrefab"/> of the block.</param>
    /// <param name="blockType">The script type of the block.</param>
    /// <param name="terminals">The terminals of the block.</param>
    public BlockDef(PartialPrefab prefab, ScriptBlockType blockType, TerminalBuilder terminals)
    {
        Prefab = prefab;
        BlockType = blockType;
        Terminals = terminals.Build(Prefab.Size, BlockType);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BlockDef"/> class.
    /// </summary>
    /// <param name="name">Name of the block.</param>
    /// <param name="id">Id of the block.</param>
    /// <param name="blockType">The script type of the block.</param>
    /// <param name="prefabType">The prefab type of the block.</param>
    /// <param name="size">Size of the block.</param>
    /// <param name="terminals">The terminals of the block.</param>
    public BlockDef(string name, ushort id, ScriptBlockType blockType, PrefabType prefabType, int3 size, TerminalBuilder terminals)
    {
        if (size.X < 1 || size.Y < 1 || size.Z < 1)
        {
            ThrowArgumentOutOfRangeException(nameof(size), $"{nameof(size)} cannot be negative or zero.");
        }

        List<PartialPrefabSegment> segments = new(size.X * size.Y * size.Z);
        for (int z = 0; z < size.Z; z++)
        {
            for (int y = 0; y < size.Y; y++)
            {
                for (int x = 0; x < size.X; x++)
                {
                    segments.Add(new PartialPrefabSegment(id, new int3(x, y, z)));
                }
            }
        }

        Prefab = new PartialPrefab(id, name, prefabType, segments);
        BlockType = blockType;
        Terminals = terminals.Build(Prefab.Size, BlockType);
    }

    /// <summary>
    /// Gets the before terminal, if <see cref="BlockType"/> is equal to <see cref="ScriptBlockType.Active"/>; otherwise, throws.
    /// </summary>
    /// <value>The before terminal.</value>
    public TerminalDef Before => BlockType == ScriptBlockType.Active ? Terminals.Get(^1) : throw new InvalidOperationException("Only active blocks have Before and After");

    /// <summary>
    /// Gets the after terminal, if <see cref="BlockType"/> is equal to <see cref="ScriptBlockType.Active"/>; otherwise, throws.
    /// </summary>
    /// <value>The after terminal.</value>
    public TerminalDef After => BlockType == ScriptBlockType.Active ? Terminals[0] : throw new InvalidOperationException("Only active blocks have Before and After");

    /// <summary>
    /// Gets the size of this block.
    /// </summary>
    /// <value>The size of this block.</value>
    public int3 Size => Prefab.Size;

    /// <summary>
    /// Gets a terminal by it's name.
    /// </summary>
    /// <param name="terminalName">Name of the terminal.</param>
    /// <returns>The terminal with the specified name.</returns>
    public TerminalDef this[string terminalName]
    {
        get
        {
            foreach (var terminal in Terminals)
            {
                if (terminal.Name == terminalName)
                {
                    return terminal;
                }
            }

            ThrowKeyNotFoundException($"This block doesn't contain a terminal with the name '{terminalName}'.");
            return null!;
        }
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowKeyNotFoundException(string paramName)
        => throw new KeyNotFoundException(paramName);
}
