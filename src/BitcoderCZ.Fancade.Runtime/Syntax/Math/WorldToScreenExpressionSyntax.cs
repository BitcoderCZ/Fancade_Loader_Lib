using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Math;

/// <summary>
/// A <see cref="SyntaxNode"/> for the world to screen prefab.
/// </summary>
public sealed class WorldToScreenExpressionSyntax : SyntaxNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WorldToScreenExpressionSyntax"/> class.
    /// </summary>
    /// <param name="prefabId">Id of the prefab this node represents.</param>
    /// <param name="position">Position of the prefab this node represents.</param>
    /// <param name="worldPos">The world pos terminal; or <see langword="null"/>, if it is not connected.</param>
    public WorldToScreenExpressionSyntax(ushort prefabId, ushort3 position, SyntaxTerminal? worldPos)
        : base(prefabId, position)
    {
        WorldPos = worldPos;
    }

    /// <summary>
    /// Gets the world pos terminal.
    /// </summary>
    /// <value>The world pos terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? WorldPos { get; }
}
