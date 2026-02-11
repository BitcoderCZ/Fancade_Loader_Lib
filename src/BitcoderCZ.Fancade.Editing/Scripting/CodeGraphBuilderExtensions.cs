// <copyright file="CodeGraphBuilderExtensions.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace BitcoderCZ.Fancade.Editing.Scripting;

/// <summary>
/// Extensions for <see cref="CodeGraph.Builder"/>.
/// </summary>
public static class CodeGraphBuilderExtensions
{
    extension(CodeGraph.Builder builder)
    {
        /// <summary>
        /// Connects a <see cref="Node.TerminalStore"/> to a <see cref="Node.Terminal"/>.
        /// </summary>
        /// <remarks>
        /// Ignores "null" parameters.
        /// </remarks>
        /// <param name="from">The source <see cref="Node.TerminalStore"/> to connect from.</param>
        /// <param name="to">The target <see cref="Node.Terminal"/> to connect to.</param>
        public void Connect(Node.TerminalStore from, Node.Terminal to)
        {
            if (from.OutCount is 0 || to.IsNull)
            {
                return;
            }

            foreach (var terminal in from.Out)
            {
                builder.Connect(terminal, to);
            }
        }

        /// <summary>
        /// Connects a <see cref="Node.Terminal"/> to a <see cref="Node.TerminalStore"/>.
        /// </summary>
        /// <remarks>
        /// Ignores "null" parameters.
        /// </remarks>
        /// <param name="from">The source <see cref="Node.Terminal"/> to connect from.</param>
        /// <param name="to">The target <see cref="Node.TerminalStore"/> to connect to.</param>
        public void Connect(Node.Terminal from, Node.TerminalStore to)
            => builder.Connect(from, to.In);

        /// <summary>
        /// Connects a <see cref="Node.TerminalStore"/> to a <see cref="Node.TerminalStore"/>.
        /// </summary>
        /// <remarks>
        /// Ignores "null" parameters.
        /// </remarks>
        /// <param name="from">The source <see cref="Node.TerminalStore"/> to connect from.</param>
        /// <param name="to">The target <see cref="Node.TerminalStore"/> to connect to.</param>
        public void Connect(Node.TerminalStore from, Node.TerminalStore to)
            => builder.Connect(from, to.In);
    }
}