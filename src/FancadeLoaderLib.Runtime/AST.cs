using MathUtils.Vectors;
using System.Collections.Immutable;

namespace FancadeLoaderLib.Runtime;

public sealed class AST
{
    private List<IActiveFunction> _entryPoints;

    private AST()
    {
    }

    public static AST Parse(PrefabList prefabs, ushort mainId, IRuntimeContext context)
    {
        return null!;
    }

    private readonly struct FunctionInstance
    {
        private readonly int3 Position;
        private readonly IFunction Function;
        private static readonly ImmutableArray<Connection> Connecions;
    }
}