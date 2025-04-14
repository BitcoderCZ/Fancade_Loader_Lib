using MathUtils.Vectors;

namespace FancadeLoaderLib.Runtime.Syntax;

public abstract class SyntaxNode
{
    private protected SyntaxNode(ushort prefabId, ushort3 position)
    {
        PrefabId = prefabId;
        Position = position;
    }

    public ushort PrefabId { get; }

    public ushort3 Position { get; }
}
