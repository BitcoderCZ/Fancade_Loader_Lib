using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Math;

/// <summary>
/// A <see cref="SyntaxNode"/> for the screen to world prefab.
/// </summary>
public sealed class ScreenToWorldExpressionSyntax : SyntaxNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScreenToWorldExpressionSyntax"/> class.
    /// </summary>
    /// <param name="prefabId">Id of the prefab this node represents.</param>
    /// <param name="position">Position of the prefab this node represents.</param>
    /// <param name="screenX">The screen x terminal; or <see langword="null"/>, if it is not connected.</param>
    /// <param name="screenY">The screen y terminal; or <see langword="null"/>, if it is not connected.</param>
    public ScreenToWorldExpressionSyntax(ushort prefabId, ushort3 position, SyntaxTerminal? screenX, SyntaxTerminal? screenY)
        : base(prefabId, position)
    {
        ScreenX = screenX;
        ScreenY = screenY;
    }

    /// <summary>
    /// Gets the screen x terminal.
    /// </summary>
    /// <value>The screen x terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? ScreenX { get; }

    /// <summary>
    /// Gets the screen y terminal.
    /// </summary>
    /// <value>The screen y terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? ScreenY { get; }
}
