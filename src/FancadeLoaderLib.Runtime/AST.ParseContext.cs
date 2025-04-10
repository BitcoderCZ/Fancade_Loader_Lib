using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Raw;
using MathUtils.Vectors;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib.Runtime;

public sealed partial class AST
{
    private sealed class ParseContext
    {
        public readonly PrefabList StockPrefabs = StockBlocks.PrefabList;

        public readonly PrefabList Prefabs;
        public readonly Prefab MainPrefab;

        public readonly FrozenDictionary<ushort, TerminalsInfo> TerminalInfos;

        internal readonly List<(ushort3 BlockPosition, byte3 TerminalPosition)> _entryPoints = [];

        internal readonly Dictionary<ushort3, FunctionInstance> _functions = [];

        private readonly IRuntimeContext _runtimeCtx;

        public ParseContext(PrefabList prefabs, ushort mainPrefabId, IRuntimeContext runtimeCtx)
        {
            Prefabs = prefabs;
            MainPrefab = Prefabs.GetPrefab(mainPrefabId);
            _runtimeCtx = runtimeCtx;

            Dictionary<ushort, TerminalsInfo> terminalInfos = new(StockPrefabs.PrefabCount + Prefabs.PrefabCount);

            foreach (var prefab in StockPrefabs.Prefabs.Concat(Prefabs.Prefabs))
            {
                int voidCount = 0;
                ImmutableArray<TerminalInfo>.Builder infoBuilder = ImmutableArray.CreateBuilder<TerminalInfo>(2);

                foreach (var (pos, settings) in prefab.Settings)
                {
                    if ((pos.X | pos.Y | pos.Z) > byte.MaxValue)
                    {
                        continue;
                    }

                    foreach (var setting in settings)
                    {
                        if (setting.Type < SettingType.VoidTerminal)
                        {
                            continue;
                        }

                        if (setting.Type == SettingType.VoidTerminal)
                        {
                            voidCount++;
                        }

                        var (type, isInput) = SettingTypeUtils.ToTerminalSignalType(setting.Type);

                        // TODO: determine using which voxels are obstructed
                        TerminalDirection dir;
                        if (type == SignalType.Void)
                        {
                            if (pos.Z == 0)
                            {
                                dir = TerminalDirection.NegativeZ;
                            }
                            else if (pos.Z % 8 == 6)
                            {
                                dir = TerminalDirection.PositiveZ;
                            }
                            else if (pos.X == 0)
                            {
                                dir = TerminalDirection.NegativeX;
                            }
                            else
                            {
                                dir = TerminalDirection.PositiveX;
                            }
                        }
                        else
                        {
                            dir = isInput ? TerminalDirection.NegativeX : TerminalDirection.PositiveX;
                        }

                        infoBuilder.Add(new TerminalInfo((byte3)pos, type, dir));
                    }
                }

                terminalInfos.Add(prefab.Id, new TerminalsInfo(infoBuilder.DrainToImmutable(), voidCount));
            }

            TerminalInfos = terminalInfos.ToFrozenDictionary();
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
                        var terminalsInfo = TerminalInfos[id];

                        if (terminalsInfo.VoidTerminalCount > 0)
                        {
                            var connections = GetConnectionsFrom(this, MainPrefab.Connections, pos, stockPrefab);
                            _functions.Add(pos, new FunctionInstance(pos, function, Unsafe.As<Connection[], ImmutableArray<Connection>>(ref connections)));
                        }
                        else
                        {
                            _functions.Add(pos, new FunctionInstance(pos, function, []));
                        }

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

            var info = TerminalInfos[MainPrefab.Blocks.GetBlockOrDefault(pos)].Terminals.FirstOrDefault(info => info.Position == voxelPos);

            // TODO: fix (default value is a valid value, so this will have to be done another way)
            return voxelPos == byte3.Zero || info != default ? GetImplicitlyConnectedTerminal(pos, info) : new RuntimeTerminal(null, voxelPos);
        }

        public bool TryGetImplicitlyConnectedTerminalPos(ushort3 pos, TerminalInfo info, out ushort3 otherBlockPos, out byte3 otherTerminalPos)
        {
            ushort3 otherPos = pos + (info.Position / 8) + info.Direction.GetOffset();

            ushort otherId = MainPrefab.Blocks.GetBlockOrDefault(otherPos);
            if (otherId == 0)
            {
                otherBlockPos = default;
                otherTerminalPos = default;
                return false;
            }

            if (StockPrefabs.TryGetSegments(otherId, out var segment) || Prefabs.TryGetSegments(otherId, out segment))
            {
                otherBlockPos = (ushort3)(otherPos - segment.PosInPrefab);
                otherTerminalPos = info.Direction switch
                {
                    TerminalDirection.PositiveX => new byte3(0, info.Position.Y, (segment.PosInPrefab.Z * 8) + (info.Position.Z % 8)),
                    TerminalDirection.PositiveZ => new byte3((segment.PosInPrefab.X * 8) + (info.Position.X % 8), info.Position.Y, 0),
                    TerminalDirection.NegativeX => new byte3(((segment.PosInPrefab.X + 1) * 8) - 2, info.Position.Y, (segment.PosInPrefab.Z * 8) + (info.Position.Z % 8)),
                    TerminalDirection.NegativeZ => new byte3((segment.PosInPrefab.X * 8) + (info.Position.X % 8), info.Position.Y, ((segment.PosInPrefab.Z + 1) * 8) - 2),
                    _ => throw new UnreachableException(),
                };

                var otherInfos = TerminalInfos[segment.PrefabId];

                byte3 otherTerminalPosLocal = otherTerminalPos;
                if (otherInfos.Terminals.Any(info => info.Position == otherTerminalPosLocal))
                {
                    return true;
                }
            }

            otherBlockPos = default;
            otherTerminalPos = default;
            return false;
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

        private RuntimeTerminal GetImplicitlyConnectedTerminal(ushort3 pos, TerminalInfo info)
        {
            ushort3 otherPos = pos + (info.Position / 8) + info.Direction.GetOffset();

            ushort otherId = MainPrefab.Blocks.GetBlockOrDefault(otherPos);
            if (otherId == 0)
            {
                return new RuntimeTerminal(null, byte3.Zero);
            }

            if (StockPrefabs.TryGetSegments(otherId, out var segment) || Prefabs.TryGetSegments(otherId, out segment))
            {
                ushort3 otherBlockPos = (ushort3)(otherPos - segment.PosInPrefab);

                otherBlockPos = (ushort3)(otherPos - segment.PosInPrefab);
                byte3 otherTerminalPos = info.Direction switch
                {
                    TerminalDirection.PositiveX => new byte3(0, info.Position.Y, (segment.PosInPrefab.Z * 8) + (info.Position.Z % 8)),
                    TerminalDirection.PositiveZ => new byte3((segment.PosInPrefab.X * 8) + (info.Position.X % 8), info.Position.Y, 0),
                    TerminalDirection.NegativeX => new byte3(((segment.PosInPrefab.X + 1) * 8) - 2, info.Position.Y, (segment.PosInPrefab.Z * 8) + (info.Position.Z % 8)),
                    TerminalDirection.NegativeZ => new byte3((segment.PosInPrefab.X * 8) + (info.Position.X % 8), info.Position.Y, ((segment.PosInPrefab.Z + 1) * 8) - 2),
                    _ => throw new UnreachableException(),
                };

                var otherInfos = TerminalInfos[segment.PrefabId];

                if (otherInfos.Terminals.Any(info => info.Position == otherTerminalPos) && TryGetOrCreateFunction(otherBlockPos, out var otherFunction))
                {
                    return new RuntimeTerminal(otherFunction, otherTerminalPos);
                }
            }

            return new RuntimeTerminal(null, byte3.Zero);
        }

        public readonly struct TerminalsInfo
        {
            public TerminalsInfo(ImmutableArray<TerminalInfo> terminals, int voidTerminalCount)
            {
                Terminals = terminals;
                VoidTerminalCount = voidTerminalCount;
            }

            public readonly ImmutableArray<TerminalInfo> Terminals { get; }

            public readonly int VoidTerminalCount { get; }

            public readonly IEnumerable<TerminalInfo> InputTerminals => Terminals.Where(info => info.IsInput);

            public readonly IEnumerable<TerminalInfo> OutputTerminals => Terminals.Where(info => !info.IsInput);
        }

        public readonly struct TerminalInfo
        {
            public TerminalInfo(byte3 position, SignalType type, TerminalDirection direction)
            {
                Position = position;
                Type = type;
                Direction = direction;
            }

            public readonly byte3 Position { get; }

            public readonly SignalType Type { get; }

            public readonly TerminalDirection Direction { get; }

            public readonly bool IsInput => Direction is TerminalDirection.NegativeX or TerminalDirection.PositiveZ;

            public static bool operator ==(TerminalInfo left, TerminalInfo right)
                => left.Position == right.Position && left.Type == right.Type && left.Direction == right.Direction;

            public static bool operator !=(TerminalInfo left, TerminalInfo right)
                => left.Position != right.Position || left.Type != right.Type || left.Direction != right.Direction;

            public override int GetHashCode()
                => HashCode.Combine(Position, Type, Direction);

            public override bool Equals(object? obj)
                => obj is TerminalInfo other && other == this;
        }
    }
}