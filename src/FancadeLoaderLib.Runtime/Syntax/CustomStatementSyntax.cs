using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace FancadeLoaderLib.Runtime.Syntax;

public sealed class CustomStatementSyntax : StatementSyntax
{
    public CustomStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, AST ast)
        : base(prefabId, position, outVoidConnections)
    {
        AST = ast;
    }

    public AST AST { get; }

    public override IEnumerable<byte3> InputVoidTerminals => AST.VoidInputs.Select(con => con.OutsidePosition);
}
