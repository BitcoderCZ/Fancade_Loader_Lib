using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Fancade.Editing.Scripting.Settings;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Game;

/// <summary>
/// A <see cref="SyntaxNode"/> for the menu item prefab.
/// </summary>
public sealed class MenuItemStatementSyntax : StatementSyntax
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MenuItemStatementSyntax"/> class.
    /// </summary>
    /// <param name="prefabId">Id of the prefab this node represents.</param>
    /// <param name="position">Position of the prefab this node represents.</param>
    /// <param name="outVoidConnections">Output void connections from this node.</param>
    /// <param name="variable">The variable terminal; or <see langword="null"/>, if it is not connected.</param>
    /// <param name="picture">The picture terminal; or <see langword="null"/>, if it is not connected.</param>
    /// <param name="name">Name of the item or section.</param>
    /// <param name="maxBuyCount">The maximum number of times the item can be bought.</param>
    /// <param name="priceIncrease">Determines how the price of the item increases.</param>
    public MenuItemStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? variable, SyntaxTerminal? picture, string name, MaxBuyCount maxBuyCount, PriceIncrease priceIncrease)
        : base(prefabId, position, outVoidConnections)
    {
        Variable = variable;
        Picture = picture;
        Name = name;
        MaxBuyCount = maxBuyCount;
        PriceIncrease = priceIncrease;
    }

    /// <summary>
    /// Gets the variable terminal.
    /// </summary>
    /// <value>The variable terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? Variable { get; }

    /// <summary>
    /// Gets the picture terminal.
    /// </summary>
    /// <value>The picture terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? Picture { get; }

    /// <summary>
    /// Gets the name of the item or section.
    /// </summary>
    /// <value>Name of the item or section.</value>
    public string Name { get; }

    /// <summary>
    /// Gets the maximum number of times the item can be bought.
    /// </summary>
    /// <value>The maximum number of times the item can be bought.</value>
    public MaxBuyCount MaxBuyCount { get; }

    /// <summary>
    /// Gets how the price of the item increases.
    /// </summary>
    /// <value>Determines how the price of the item increases.</value>
    public PriceIncrease PriceIncrease { get; }

    /// <inheritdoc/>
    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(2)];
}
