// <copyright file="PlaySensorStatementSyntax.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Control;

/// <summary>
/// A <see cref="SyntaxNode"/> for the play sensor prefab.
/// </summary>
public sealed class PlaySensorStatementSyntax : StatementSyntax
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlaySensorStatementSyntax"/> class.
    /// </summary>
    /// <inheritdoc cref="StatementSyntax(ushort, int3, ImmutableArray{Connection})"/>
    public PlaySensorStatementSyntax(int3 position, ImmutableArray<Connection> outVoidConnections)
        : base(238, position, outVoidConnections)
    {
    }

    /// <inheritdoc/>
    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(2)];
}
