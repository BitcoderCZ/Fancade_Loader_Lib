// <copyright file="IfStatementSyntax.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Control;

/// <summary>
/// A <see cref="SyntaxNode"/> for the if prefab.
/// </summary>
public sealed class IfStatementSyntax : StatementSyntax
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IfStatementSyntax"/> class.
    /// </summary>
    /// <param name="position">Position of the prefab this node represents.</param>
    /// <param name="outVoidConnections">Output void connections from this node.</param>
    /// <param name="condition">The condition terminal; or <see langword="null"/>, if it is not connected.</param>
    public IfStatementSyntax(int3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? condition)
        : base(234, position, outVoidConnections)
    {
        Condition = condition;
    }

    /// <summary>
    /// Gets the condition terminal.
    /// </summary>
    /// <value>The condition terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? Condition { get; }

    /// <inheritdoc/>
    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(2)];
}
