// <copyright file="BreakBlockCache.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Editing.Scripting.TerminalStores;
using System;
using System.Diagnostics.CodeAnalysis;
using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib.Editing.Scripting.Utils;

public sealed class BreakBlockCache
{
    private readonly int _maxUsesPerAxis;

    private Block? _lastBlock;
    [SuppressMessage("Performance", "CA1805:Do not initialize unnecessarily", Justification = "Clarity.")]
    private bool _invalid = false;

    private int _xUseCount;
    private int _yUseCount;
    private int _zUseCount;

    public BreakBlockCache(Block? breakBlock, int maxUsesPerAxis)
    {
        if (maxUsesPerAxis < 1)
        {
            ThrowArgumentOutOfRangeException(nameof(maxUsesPerAxis), $"{nameof(maxUsesPerAxis)} must be greater than 0.");
        }
        else if (maxUsesPerAxis > FancadeConstants.MaxWireSplits)
        {
            ThrowArgumentOutOfRangeException(nameof(maxUsesPerAxis), $"{nameof(maxUsesPerAxis)} must be smaller than {FancadeConstants.MaxWireSplits}.");
        }

        _lastBlock = ValidateBlock(breakBlock, nameof(breakBlock));
        _maxUsesPerAxis = maxUsesPerAxis;
    }

    public void SetNewBlock(Block breakBlock)
    {
        _lastBlock = ValidateBlock(breakBlock, nameof(breakBlock));
        _invalid = false;
        _xUseCount = 0;
        _yUseCount = 0;
        _zUseCount = 0;
    }

    public bool CanGet()
        => _lastBlock is not null && !_invalid;

    public bool TryGet([NotNullWhen(true)] out Block? breakBlock)
    {
        if (_lastBlock is not null &&
            CheckAndInc(0) &&
            CheckAndInc(1) &&
            CheckAndInc(2))
        {
            breakBlock = _lastBlock;
            return true;
        }
        else
        {
            breakBlock = null;
            return false;
        }
    }

    public bool TryGetAxis(int axis, [NotNullWhen(true)] out ITerminalStore? store)
    {
        if (_lastBlock is null)
        {
            store = null;
            return false;
        }

        if (axis < 0 || axis > 2)
        {
            ThrowArgumentOutOfRangeException(nameof(axis));
        }

        if (CheckAndInc(axis))
        {
            // x - 2, y - 1, z - 0
            store = TerminalStore.CreateOut(_lastBlock, _lastBlock.Type.Terminals[2 - axis]);
            return true;
        }
        else
        {
            store = null;
            return false;
        }
    }

    private static Block? ValidateBlock(Block? breakBlock, string argumentName)
    {
        if (breakBlock is null)
        {
            return breakBlock;
        }

        BlockDef type = breakBlock.Type;
        return type == StockBlocks.Math.Break_Vector || type == StockBlocks.Math.Break_Rotation
            ? breakBlock
            : throw new ArgumentException(argumentName, $"{argumentName} must be {nameof(StockBlocks.Math.Break_Vector)} or {nameof(StockBlocks.Math.Break_Rotation)}.");
    }

    private bool CheckAndInc(int axis)
    {
        if (_lastBlock is null || _invalid)
        {
            return false;
        }

#pragma warning disable SA1119 // Statement should not use unnecessary parenthesis - but they are necesarry here... wtf
        _invalid = !(axis switch
        {
            0 => _xUseCount++ < _maxUsesPerAxis,
            1 => _yUseCount++ < _maxUsesPerAxis,
            2 => _zUseCount++ < _maxUsesPerAxis,
            _ => false,
        });
#pragma warning restore SA1119

        return !_invalid;
    }
}
