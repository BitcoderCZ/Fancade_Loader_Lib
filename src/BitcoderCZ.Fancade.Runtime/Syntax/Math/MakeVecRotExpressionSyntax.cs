using BitcoderCZ.Maths.Vectors;
using static BitcoderCZ.Fancade.Utils.ThrowHelper;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Math;

/// <summary>
/// A <see cref="SyntaxNode"/> for the make vector/rotation prefab.
/// </summary>
public sealed class MakeVecRotExpressionSyntax : SyntaxNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MakeVecRotExpressionSyntax"/> class.
    /// </summary>
    /// <param name="prefabId">Id of the prefab this node represents.</param>
    /// <param name="position">Position of the prefab this node represents.</param>
    /// <param name="x">The x terminal; or <see langword="null"/>, if it is not connected.</param>
    /// <param name="y">The y terminal; or <see langword="null"/>, if it is not connected.</param>
    /// <param name="z">The z terminal; or <see langword="null"/>, if it is not connected.</param>
    public MakeVecRotExpressionSyntax(ushort prefabId, int3 position, SyntaxTerminal? x, SyntaxTerminal? y, SyntaxTerminal? z)
        : base(prefabId, position)
    {
        if (prefabId is not (150 or 162))
        {
            ThrowArgumentOutOfRangeException(nameof(prefabId), $"{nameof(prefabId)} must be 150 or 162.");
        }

        X = x;
        Y = y;
        Z = z;
    }

    /// <summary>
    /// Gets the x terminal.
    /// </summary>
    /// <value>The x terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? X { get; }

    /// <summary>
    /// Gets the y terminal.
    /// </summary>
    /// <value>The y terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? Y { get; }

    /// <summary>
    /// Gets the z terminal.
    /// </summary>
    /// <value>The z terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? Z { get; }
}
