using FancadeLoaderLib.Runtime.Syntax;
using MathUtils.Vectors;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib.Runtime;

public sealed partial class AST
{
    public readonly ushort PrefabId;

    public readonly List<(ushort3 BlockPosition, byte3 TerminalPosition)> EntryPoints;

    public readonly Dictionary<ushort3, SyntaxNode> Nodes;

    public readonly ImmutableArray<Variable> GlobalVariables;

    public readonly FrozenDictionary<ushort, ImmutableArray<Variable>> Variables;

    public AST(ushort prefabId, List<(ushort3 BlockPosition, byte3 TerminalPosition)> entryPoints, Dictionary<ushort3, SyntaxNode> nodes, ImmutableArray<Variable> globalVariables, FrozenDictionary<ushort, ImmutableArray<Variable>> variables)
    {
        ThrowIfNull(entryPoints, nameof(entryPoints));
        ThrowIfNull(nodes, nameof(nodes));
        ThrowIfNull(globalVariables, nameof(globalVariables));
        ThrowIfNull(variables, nameof(variables));

        PrefabId = prefabId;
        EntryPoints = entryPoints;
        Nodes = nodes;
        GlobalVariables = globalVariables;
        Variables = variables;
    }

    public static AST Parse(PrefabList prefabs, ushort mainPrefabId)
    {
        ThrowIfNull(prefabs, nameof(prefabs));

        var ctx = new GlobalParseContext(prefabs, mainPrefabId);

        return ctx.Parse();
    }

    private static IEnumerable<Connection> GetConnectionsTo(List<Connection> connections, ushort3 pos)
    {
        for (int i = 0; i < connections.Count; i++)
        {
            if (connections[i].To == pos)
            {
                yield return connections[i];
            }
        }
    }

    private static IEnumerable<Connection> GetConnectionsFrom(List<Connection> connections, ushort3 pos)
    {
        for (int i = 0; i < connections.Count; i++)
        {
            if (connections[i].From == pos)
            {
                yield return connections[i];
            }
        }
    }

    public readonly struct FunctionInstance
    {
        public readonly ushort3 Position;
        public readonly IFunction Function;

        // only for active functions
        public readonly ImmutableArray<Connection> Connections;

        public FunctionInstance(ushort3 position, IFunction function, ImmutableArray<Connection> connections)
        {
#if DEBUG
            foreach (var connection in connections)
            {
                Debug.Assert(connection.From == position, $"{nameof(connection)}.{nameof(Connection.From)} should be equal to {nameof(Position)}.");
            }
#endif

            Position = position;
            Function = function;
            Connections = connections;
        }
    }
}