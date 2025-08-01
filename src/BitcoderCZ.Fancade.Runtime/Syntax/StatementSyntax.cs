﻿using BitcoderCZ.Maths.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax;

public abstract class StatementSyntax : SyntaxNode
{
    private protected StatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections)
        : base(prefabId, position)
    {
        OutVoidConnections = outVoidConnections;
    }

    public ImmutableArray<Connection> OutVoidConnections { get; }

    public abstract IEnumerable<byte3> InputVoidTerminals { get; }
}