// <copyright file="TerminalUtils.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using static BitcoderCZ.Fancade.Editing.Scripting.CodeWriter;
using Terminal = BitcoderCZ.Fancade.Editing.Scripting.Node.Terminal;

namespace BitcoderCZ.Fancade.Editing.Utils;

/// <summary>
/// Utils for <see cref="Terminal"/>.
/// </summary>
public static class TerminalUtils
{
    /// <summary>
    /// Wraps an <see cref="Terminal"/> as an <see cref="IExpression"/>.
    /// </summary>
    /// <param name="terminal">The terminal to wrap.</param>
    /// <returns><paramref name="terminal"/> wrapped in an <see cref="IExpression"/>.</returns>
    public static IExpression Wrap(this Terminal terminal)
        => new Expressions.TerminalWrapperExpression(terminal);
}
