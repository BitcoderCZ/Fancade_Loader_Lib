using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade.Runtime.Syntax;

/// <summary>
/// Represents a node in <see cref="FcAST"/>.
/// </summary>
public abstract class SyntaxNode
{
    private protected SyntaxNode(ushort prefabId, ushort3 position)
    {
        PrefabId = prefabId;
        Position = position;
    }

    /// <summary>
    /// Gets the id of the prefab this node represents.
    /// </summary>
    /// <value>Id of the prefab this node represents.</value>
    public ushort PrefabId { get; }

    /// <summary>
    /// Gets the position of the prefab this node represents.
    /// </summary>
    /// <value>Position of the prefab this node represents.</value>
    public ushort3 Position { get; }
}
