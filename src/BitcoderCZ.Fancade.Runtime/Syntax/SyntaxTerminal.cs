using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade.Runtime.Syntax;

/// <summary>
/// Represents a <see cref="SyntaxNode"/>'s terminal.
/// </summary>
public sealed class SyntaxTerminal
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SyntaxTerminal"/> class.
    /// </summary>
    /// <param name="node">The <see cref="SyntaxNode"/> this terminal belongs to.</param>
    /// <param name="position">Voxel position of this terminal.</param>
    public SyntaxTerminal(SyntaxNode node, byte3 position)
    {
        Node = node;
        Position = position;
    }

    /// <summary>
    /// Gets the <see cref="SyntaxNode"/> this terminal belongs to.
    /// </summary>
    /// <value><see cref="SyntaxNode"/> this terminal belongs to.</value>
    public SyntaxNode Node { get; }

    /// <summary>
    /// Gets the voxel position of this terminal.
    /// </summary>
    /// <value>Voxel position of this terminal.</value>
    public byte3 Position { get; }
}
