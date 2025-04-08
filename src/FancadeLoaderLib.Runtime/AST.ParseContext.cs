using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Raw;
using MathUtils.Vectors;
using System.Diagnostics.CodeAnalysis;
using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib.Runtime;

public sealed partial class AST
{
    public sealed class ParseContext
    {
        public readonly PrefabList StockPrefabs = StockBlocks.PrefabList;

        public readonly PrefabList Prefabs;
        public readonly Prefab MainPrefab;

        internal readonly List<(ushort3 BlockPosition, byte3 TerminalPosition)> _entryPoints = [];

        internal readonly Dictionary<ushort3, FunctionInstance> _functions = [];

        private readonly IRuntimeContext _runtimeCtx;

        public ParseContext(PrefabList prefabs, ushort mainPrefabId, IRuntimeContext runtimeCtx)
        {
            Prefabs = prefabs;
            MainPrefab = Prefabs.GetPrefab(mainPrefabId);
            _runtimeCtx = runtimeCtx;
        }

        public bool TryCreateFunction(ushort3 pos, [MaybeNullWhen(false)] out IFunction function)
        {
            if (_functions.ContainsKey(pos))
            {
                function = null;
                return false;
            }

            ushort id = MainPrefab.Blocks.GetBlockUnchecked(pos);

            if (id != 0 && id < RawGame.CurrentNumbStockPrefabs)
            {
                if (StockPrefabs.TryGetPrefab(id, out var stockPrefab) && stockPrefab.Settings.Count > 0)
                {
                    function = FunctionCreation.CreateFunction(id, pos, this);

                    if (function is not null)
                    {
                        _functions.Add(pos, new FunctionInstance(pos, function, [.. GetConnectionsFrom(MainPrefab.Connections, pos)]));

                        if (function is IActiveFunction && GetConnectedTerminal(pos, TerminalDef.GetBeforePosition(stockPrefab.Size.Z)).Function is null)
                        {
                            _entryPoints.Add((pos, TerminalDef.GetBeforePosition(stockPrefab.Size.Z)));
                        }

                        return true;
                    }
                }
            }
            else
            {
                if (Prefabs.TryGetPrefab(id, out var prefab) && prefab.Settings.Count > 0)
                {
                    ThrowNotImplementedException("Custom script blocks have not yet been implemented.");
                }
            }

            function = null;
            return false;
        }

        public bool TryGetOrCreateFunction(ushort3 pos, [MaybeNullWhen(false)] out IFunction function)
        {
            if (_functions.TryGetValue(pos, out var instance))
            {
                function = instance.Function;
                return true;
            }

            return TryCreateFunction(pos, out function);
        }

        public RuntimeTerminal GetTerminal(ushort3 pos, byte3 voxelPos)
            => TryGetOrCreateFunction(pos, out var function)
            ? new RuntimeTerminal(function, voxelPos)
            : new RuntimeTerminal(null, voxelPos);

        public RuntimeTerminal GetConnectedTerminal(ushort3 pos, byte3 voxelPos)
        {
            foreach (var connection in GetConnectionsTo(MainPrefab.Connections, pos))
            {
                if (connection.ToVoxel == voxelPos)
                {
                    return GetTerminal(connection.From, (byte3)connection.FromVoxel);
                }
            }

            return new RuntimeTerminal(null, byte3.Zero);
        }

        public bool TryGetSettingOfType(ushort3 pos, int index, SettingType type, [MaybeNullWhen(false)] out object value)
        {
            if (MainPrefab.Settings.TryGetValue(pos, out var settings))
            {
                foreach (var setting in settings)
                {
                    if (setting.Index == index && setting.Type == type)
                    {
                        value = setting.Value;
                        return true;
                    }
                }
            }

            value = null;
            return false;
        }

        public int GetVariableId(string name, SignalType type)
            => _runtimeCtx.GetVariableId(new Variable(name, type));
    }
}