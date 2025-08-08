using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Math;

// the opeation is determined by PrefabId

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
    public BinaryExpressionSyntax(ushort prefabId, ushort3 position, SyntaxTerminal? input1, SyntaxTerminal? input2)
        : base(prefabId, position)
    {
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
