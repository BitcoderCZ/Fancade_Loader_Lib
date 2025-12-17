// <copyright file="LateUpdateStatementSyntax.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Control;

/// <summary>
/// A <see cref="SyntaxNode"/> for the late update prefab.
/// </summary>
public sealed class LateUpdateStatementSyntax : StatementSyntax
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LateUpdateStatementSyntax"/> class.
    /// </summary>
    /// <inheritdoc cref="StatementSyntax(ushort, int3, ImmutableArray{Connection})"/>
    public LateUpdateStatementSyntax(int3 position, ImmutableArray<Connection> outVoidConnections)
        : base(566, position, outVoidConnections)
    {
    }

    /// <inheritdoc/>
    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(2)];
}
