// <copyright file="TerminalUtils.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Fancade.Editing.Scripting.Terminals;
using static BitcoderCZ.Fancade.Editing.Scripting.CodeWriter;

namespace BitcoderCZ.Fancade.Editing.Utils;

/// <summary>
/// Utils for <see cref="ITerminal"/>.
/// </summary>
public static class TerminalUtils
{
    /// <summary>
    /// Wraps an <see cref="ITerminal"/> as an <see cref="IExpression"/>.
    /// </summary>
    /// <param name="terminal">The terminal to wrap.</param>
    /// <returns><paramref name="terminal"/> wrapped in an <see cref="IExpression"/>.</returns>
    public static IExpression Wrap(this ITerminal terminal)
        => new Expressions.TerminalWrapperExpression(terminal);
}
