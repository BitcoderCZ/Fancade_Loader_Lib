using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Fancade.Editing.Scripting.Settings;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Game;

public sealed class MenuItemStatementSyntax : StatementSyntax
{
    public MenuItemStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? variable, SyntaxTerminal? picture, string name, MaxBuyCount maxBuyCount, PriceIncrease priceIncrease)
        : base(prefabId, position, outVoidConnections)
    {
        Variable = variable;
        Picture = picture;
        Name = name;
        MaxBuyCount = maxBuyCount;
        PriceIncrease = priceIncrease;
    }

    public SyntaxTerminal? Variable { get; }

    public SyntaxTerminal? Picture { get; }

    public string Name { get; }

    public MaxBuyCount MaxBuyCount { get; }

    public PriceIncrease PriceIncrease { get; }

    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(2)];
}
