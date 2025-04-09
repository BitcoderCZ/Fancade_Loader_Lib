using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Raw;
using MathUtils.Vectors;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
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
                        var connections = GetConnectionsFrom(MainPrefab.Connections, pos, stockPrefab);
                        _functions.Add(pos, new FunctionInstance(pos, function, Unsafe.As<Connection[], ImmutableArray<Connection>>(ref connections)));

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

            return GetImplicitlyConnectedTerminal(pos, voxelPos);

            RuntimeTerminal GetImplicitlyConnectedTerminal(ushort3 pos, byte3 voxelPos)
            {
                ushort3 otherPos = pos + (voxelPos / 8);
                if (voxelPos.Z == 0)
                {
                    otherPos.Z--;
                }
                else if (voxelPos.X == 0)
                {
                    otherPos.X--;
                }
                else
                {
                    otherPos.Z++;
                }

                ushort otherId = MainPrefab.Blocks.GetBlockOrDefault(otherPos);
                if (otherId == 0)
                {
                    return new RuntimeTerminal(null, byte3.Zero);
                }

                if (StockPrefabs.TryGetSegments(otherId, out var segment) || Prefabs.TryGetSegments(otherId, out segment))
                {
                    if (TryGetOrCreateFunction((ushort3)(otherPos - segment.PosInPrefab), out var otherFunction))
                    {
#pragma warning disable IDE0046 // Convert to conditional expression
                        if (voxelPos.Z == 0)
                        {
                            return new RuntimeTerminal(otherFunction, new byte3((segment.PosInPrefab.X * 8) + (voxelPos.X % 8), voxelPos.Y, ((segment.PosInPrefab.Z + 1) * 8) - 2));
                        }
                        else if (voxelPos.X == 0)
                        {
                            return new RuntimeTerminal(otherFunction, new byte3(((segment.PosInPrefab.X + 1) * 8) - 2, voxelPos.Y, (segment.PosInPrefab.Z * 8) + (voxelPos.Z % 8)));
                        }
                        else
                        {
                            return new RuntimeTerminal(otherFunction, new byte3((segment.PosInPrefab.X * 8) + (voxelPos.X % 8), voxelPos.Y, 0));
                        }
#pragma warning restore IDE0046 // Convert to conditional expression
                    }
                }

                return new RuntimeTerminal(null, byte3.Zero);
            }
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