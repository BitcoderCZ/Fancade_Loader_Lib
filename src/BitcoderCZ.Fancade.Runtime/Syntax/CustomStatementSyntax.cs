using BitcoderCZ.Fancade;
using MathUtils.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax;

public sealed class CustomStatementSyntax : StatementSyntax
{
    public CustomStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, AST ast, ImmutableArray<(byte3 TerminalPosition, SyntaxTerminal? ConnectedTerminal)> connectedInputTerminals)
        : base(prefabId, position, outVoidConnections)
    {
        AST = ast;
        ConnectedInputTerminals = connectedInputTerminals;
    }

    public AST AST { get; }

    public ImmutableArray<(byte3 TerminalPosition, SyntaxTerminal? ConnectedTerminal)> ConnectedInputTerminals { get; }

    public override IEnumerable<byte3> InputVoidTerminals => AST.VoidInputs.Select(con => con.OutsidePosition);
}
