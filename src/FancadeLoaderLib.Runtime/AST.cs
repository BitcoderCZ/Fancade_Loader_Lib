using MathUtils.Vectors;
using System.Collections.Immutable;
using System.Diagnostics;
using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib.Runtime;

public sealed partial class AST
{
    public readonly List<(ushort3 BlockPosition, byte3 TerminalPosition)> EntryPoints;

    public readonly Dictionary<ushort3, FunctionInstance> Functions;

    public readonly IRuntimeContext RuntimeContext;

    public AST(List<(ushort3 BlockPosition, byte3 TerminalPosition)> entryPoints, Dictionary<ushort3, FunctionInstance> functions, IRuntimeContext runtimeContext)
    {
        ThrowIfNull(entryPoints, nameof(entryPoints));
        ThrowIfNull(functions, nameof(functions));

        EntryPoints = entryPoints;
        Functions = functions;
        RuntimeContext = runtimeContext;
    }

    public static AST Parse(PrefabList prefabs, ushort mainPrefabId, IRuntimeContext runtimeContext)
    {
        ThrowIfNull(prefabs, nameof(prefabs));
        ThrowIfNull(runtimeContext, nameof(runtimeContext));

        var ctx = new ParseContext(prefabs, mainPrefabId, runtimeContext);

        var blocks = ctx.MainPrefab.Blocks;

        HashSet<Variable> variables = [];

        for (int z = blocks.Size.Z - 1; z >= 0; z--)
        {
            for (int y = blocks.Size.Y - 1; y >= 0; y--)
            {
                for (int x = 0; x < blocks.Size.X; x++)
                {
                    ushort3 pos = new ushort3(x, y, z);

                    ushort id = blocks.GetBlockUnchecked(pos);

                    if (id is 46 or 48 or 50 or 52 or 54 or 56)
                    {
                        // get variable
                        if (ctx.TryGetSettingOfType(pos, 0, SettingType.String, out object? variableName))
                        {
                            variables.Add(new Variable((string)variableName, (SignalType)((id - 46) + 2)));
                        }
                    }
                    else if (id is 428 or 430 or 432 or 434 or 436 or 438)
                    {
                        // set variable
                        if (ctx.TryGetSettingOfType(pos, 0, SettingType.String, out object? variableName))
                        {
                            variables.Add(new Variable((string)variableName, (SignalType)((id - 428) + 2)));
                        }
                    }
                }
            }
        }

        runtimeContext.Init(variables);

        // order matters for entryPoints
        for (int z = blocks.Size.Z - 1; z >= 0; z--)
        {
            for (int y = blocks.Size.Y - 1; y >= 0; y--)
            {
                for (int x = 0; x < blocks.Size.X; x++)
                {
                    ushort3 pos = new ushort3(x, y, z);

                    if (blocks.GetBlockUnchecked(pos) != 0)
                    {
                        ctx.TryCreateFunction(pos, out _);
                    }
                }
            }
        }

        return new AST(ctx._entryPoints, ctx._functions, runtimeContext);
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