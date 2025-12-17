// <copyright file="SwipeSensorStatementSyntax.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Control;

/// <summary>
/// A <see cref="SyntaxNode"/> for the swipe sensor prefab.
/// </summary>
public sealed class SwipeSensorStatementSyntax : StatementSyntax
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SwipeSensorStatementSyntax"/> class.
    /// </summary>
    /// <inheritdoc cref="StatementSyntax(ushort, int3, ImmutableArray{Connection})"/>
    public SwipeSensorStatementSyntax(int3 position, ImmutableArray<Connection> outVoidConnections)
        : base(248, position, outVoidConnections)
    {
    }

    /// <inheritdoc/>
    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(2)];
}
