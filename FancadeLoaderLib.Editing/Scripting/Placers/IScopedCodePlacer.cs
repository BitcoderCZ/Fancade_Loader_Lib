// <copyright file="IScopedCodePlacer.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Editing.Utils;
using System;
using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib.Editing.Scripting.Placers;

/// <summary>
/// <see cref="ICodePlacer"/> with code blocks.
/// </summary>
public interface IScopedCodePlacer : ICodePlacer
{
    /// <summary>
    /// Gets the number of blocks in the current scope.
    /// </summary>
    /// <value>The number of blocks in the current scope.</value>
    int CurrentCodeBlockBlocks { get; }

    /// <summary>
    /// Enters a statement block.
    /// </summary>
    void EnterStatementBlock();

    /// <summary>
    /// Exits a statement block.
    /// </summary>
    void ExitStatementBlock();

    /// <summary>
    /// Enters an expression block.
    /// </summary>
    void EnterExpressionBlock();

    /// <summary>
    /// Exits an expression block.
    /// </summary>
    void ExitExpressionBlock();

    /// <summary>
    /// Enters an highlight block.
    /// </summary>
    /// <remarks>
    /// Blocks that are placed while in highlight block will be placed in a position that is easily visible.
    /// </remarks>
    void EnterHighlight();

    /// <summary>
    /// Exits an highlight block.
    /// </summary>
    void ExitHightlight();
}

/// <summary>
/// Utils for <see cref="IScopedCodePlacer"/>.
/// </summary>
public static class IScopedCodePlacerUtils
{
    /// <summary>
    /// Enters a statement block.
    /// </summary>
    /// <param name="placer">The <see cref="IScopedCodePlacer"/> to use.</param>
    /// <returns>An <see cref="IDisposable"/>, that when disposed exits the statement block.</returns>
    public static IDisposable StatementBlock(this IScopedCodePlacer placer)
    {
        ThrowIfNull(placer, nameof(placer));

        placer.EnterStatementBlock();
        return new Disposable(placer.ExitStatementBlock);
    }

    /// <summary>
    /// Enters an expression block.
    /// </summary>
    /// <param name="placer">The <see cref="IScopedCodePlacer"/> to use.</param>
    /// <returns>An <see cref="IDisposable"/>, that when disposed exits the expression block.</returns>
    public static IDisposable ExpressionBlock(this IScopedCodePlacer placer)
    {
        ThrowIfNull(placer, nameof(placer));

        placer.EnterExpressionBlock();
        return new Disposable(placer.ExitExpressionBlock);
    }

    /// <summary>
    /// Enters a highlight block.
    /// </summary>
    /// <param name="placer">The <see cref="IScopedCodePlacer"/> to use.</param>
    /// <returns>An <see cref="IDisposable"/>, that when disposed exits the highlight block.</returns>
    public static IDisposable HighlightBlock(this IScopedCodePlacer placer)
    {
        ThrowIfNull(placer, nameof(placer));

        placer.EnterHighlight();
        return new Disposable(placer.ExitHightlight);
    }
}