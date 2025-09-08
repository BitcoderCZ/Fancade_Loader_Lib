using BitcoderCZ.Maths.Vectors;
using static BitcoderCZ.Fancade.Utils.ThrowHelper;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Math;

// the operation is determined by PrefabId

/// <summary>
/// A <see cref="SyntaxNode"/> for any binary math prefab.
/// </summary>
public sealed class BinaryExpressionSyntax : SyntaxNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BinaryExpressionSyntax"/> class.
    /// </summary>
    /// <param name="prefabId">Id of the prefab this node represents.</param>
    /// <param name="position">Position of the prefab this node represents.</param>
    /// <param name="input1">The first input terminal; or <see langword="null"/>, if it is not connected.</param>
    /// <param name="input2">The second input terminal; or <see langword="null"/>, if it is not connected.</param>
    public BinaryExpressionSyntax(ushort prefabId, int3 position, SyntaxTerminal? input1, SyntaxTerminal? input2)
        : base(prefabId, position)
    {
        if (prefabId is not (92 or 96 or 100 or 104 or 108 or 112 or 116 or 120 or 124 or 172 or 457 or 132 or 136 or 140 or 421 or 146 or 417 or 128 or 481 or 168 or 176 or 180 or 580 or 570 or 574 or 190 or 200 or 204))
        {
            ThrowArgumentOutOfRangeException(nameof(prefabId), $"{nameof(prefabId)} must be 92 or 96 or 100 or 104 or 108 or 112 or 116 or 120 or 124 or 172 or 457 or 132 or 136 or 140 or 421 or 146 or 417 or 128 or 481 or 168 or 176 or 180 or 580 or 570 or 574 or 190 or 200 or 204.");
        }

        Input1 = input1;
        Input2 = input2;
    }

    /// <summary>
    /// Gets the first input terminal.
    /// </summary>
    /// <value>The first input terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? Input1 { get; }

    /// <summary>
    /// Gets the second input terminal.
    /// </summary>
    /// <value>The second input terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? Input2 { get; }
}
