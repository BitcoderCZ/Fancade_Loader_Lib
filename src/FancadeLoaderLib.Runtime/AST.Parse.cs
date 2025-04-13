using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Raw;
using FancadeLoaderLib.Runtime.Syntax;
using MathUtils.Vectors;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib.Runtime;

public sealed partial class AST
{
    private sealed class GlobalParseContext
    {
        public readonly PrefabList StockPrefabs = StockBlocks.PrefabList;

        public readonly PrefabList Prefabs;

        public readonly ImmutableArray<Variable> GlobalVariables;

        public readonly FrozenDictionary<ushort, ParsePrefabInfo> PrefabInfos;
        public readonly ushort MainId;

        public GlobalParseContext(PrefabList prefabs, ushort mainId)
        {
            Prefabs = prefabs;
            MainId = mainId;

            Dictionary<ushort, ParsePrefabInfo> prefabInfos = new(StockPrefabs.PrefabCount + Prefabs.PrefabCount);

            HashSet<Variable> variables = [];

            foreach (var prefab in StockPrefabs.Concat(prefabs))
            {
                var ctx = new ParseContext(
                        this,
                        prefab,
                        prefab.Id < RawGame.CurrentNumbStockPrefabs || (prefab.Type == PrefabType.Level && prefab.Id != MainId));

                prefabInfos.Add(prefab.Id, new(PrefabTerminalInfo.Create(prefab), ctx));

                var blocks = prefab.Blocks;
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
                                if (ctx.TryGetSettingOfType(pos, 0, SettingType.String, out object? variableNameObj))
                                {
                                    string varName = (string)variableNameObj;

                                    if (varName.StartsWith('$') || varName.StartsWith('!'))
                                    {
                                        variables.Add(new Variable(varName, (SignalType)((id - 46) + 2)));
                                    }
                                }
                            }
                            else if (id is 428 or 430 or 432 or 434 or 436 or 438)
                            {
                                // set variable
                                if (ctx.TryGetSettingOfType(pos, 0, SettingType.String, out object? variableNameObj))
                                {
                                    string varName = (string)variableNameObj;

                                    if (varName.StartsWith('$') || varName.StartsWith('!'))
                                    {
                                        variables.Add(new Variable(varName, (SignalType)((id - 428) + 2)));
                                    }
                                }
                            }
                        }
                    }
                }
            }

            PrefabInfos = prefabInfos.ToFrozenDictionary();

            GlobalVariables = [.. variables];
        }

        public AST Parse()
        {
            var ctx = PrefabInfos[MainId].ParseCtx;
            ctx.ParseAll();

            return new(MainId, ctx._entryPoints, ctx._nodes, GlobalVariables, PrefabInfos.ToFrozenDictionary(item => item.Key, item => item.Value.ParseCtx._variables));
        }
    }

    private sealed class ParseContext
    {
        public readonly Prefab Prefab;

        internal readonly List<(ushort3 BlockPosition, byte3 TerminalPosition)> _entryPoints = [];

        internal readonly Dictionary<ushort3, SyntaxNode> _nodes = [];

        internal readonly ImmutableArray<Variable> _variables;

        private readonly GlobalParseContext _globalCtx;

        private readonly bool _isDummy;

        public ParseContext(GlobalParseContext globalCtx, Prefab prefab, bool isDummy)
        {
            _globalCtx = globalCtx;
            Prefab = prefab;
            _isDummy = isDummy;

            if (!_isDummy)
            {
                HashSet<Variable> variables = [];

                var blocks = Prefab.Blocks;
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
                                string varName;
                                if (TryGetSettingOfType(pos, 0, SettingType.String, out object? variableNameObj))
                                {
                                    varName = (string)variableNameObj;

                                    if (varName.StartsWith('$') || varName.StartsWith('!'))
                                    {
                                        continue;
                                    }
                                }
                                else
                                {
                                    varName = string.Empty;
                                }

                                variables.Add(new Variable(varName, (SignalType)((id - 46) + 2)));
                            }
                            else if (id is 428 or 430 or 432 or 434 or 436 or 438)
                            {
                                // set variable
                                string varName;
                                if (TryGetSettingOfType(pos, 0, SettingType.String, out object? variableNameObj))
                                {
                                    varName = (string)variableNameObj;

                                    if (varName.StartsWith('$') || varName.StartsWith('!'))
                                    {
                                        continue;
                                    }
                                }
                                else
                                {
                                    varName = string.Empty;
                                }

                                variables.Add(new Variable(varName, (SignalType)((id - 428) + 2)));
                            }
                        }
                    }
                }

                _variables = [.. variables];
            }
            else
            {
                _variables = [];
            }
        }

        public void ParseAll()
        {
            var blocks = Prefab.Blocks;

            for (int z = blocks.Size.Z - 1; z >= 0; z--)
            {
                for (int y = blocks.Size.Y - 1; y >= 0; y--)
                {
                    for (int x = 0; x < blocks.Size.X; x++)
                    {
                        ushort3 pos = new ushort3(x, y, z);

                        if (blocks.GetBlockUnchecked(pos) != 0)
                        {
                            TryCreateNode(pos, out _);
                        }
                    }
                }
            }
        }

        public bool TryCreateNode(ushort3 pos, [MaybeNullWhen(false)] out SyntaxNode node)
        {
            if (_nodes.ContainsKey(pos))
            {
                node = null;
                return false;
            }

            ushort id = Prefab.Blocks.GetBlockUnchecked(pos);

            if (id != 0 && id < RawGame.CurrentNumbStockPrefabs)
            {
                if (_globalCtx.StockPrefabs.TryGetPrefab(id, out var stockPrefab) && stockPrefab.Settings.Count > 0)
                {
                    node = NodeCreation.CreateNode(id, pos, this);

                    if (node is not null)
                    {
                        _nodes.Add(pos, node);

                        if (node is StatementSyntax statement)
                        {
                            foreach (var termPos in statement.InputVoidTerminals)
                            {
                                if (GetConnectedTerminal(pos, termPos) is null)
                                {
                                    _entryPoints.Add((pos, termPos));
                                }
                            }
                        }

                        return true;
                    }
                }
            }
            else
            {
                if (_globalCtx.Prefabs.TryGetPrefab(id, out var prefab) && prefab.Blocks.Size != int3.Zero)
                {
                    ThrowNotImplementedException("Custom script blocks have not yet been implemented.");
                }
            }

            node = null;
            return false;
        }

        public bool TryGetOrCreateNode(ushort3 pos, [MaybeNullWhen(false)] out SyntaxNode node)
            => _nodes.TryGetValue(pos, out node) || TryCreateNode(pos, out node);

        public SyntaxNode? GetNode(ushort3 pos)
            => TryGetOrCreateNode(pos, out var node) ? node : null;

        public SyntaxTerminal? GetTerminal(ushort3 pos, byte3 voxelPos)
            => TryGetOrCreateNode(pos, out var node)
            ? new SyntaxTerminal(node, voxelPos)
            : null;

        public SyntaxTerminal? GetConnectedTerminal(ushort3 pos, byte3 voxelPos)
        {
            foreach (var connection in GetConnectionsTo(Prefab.Connections, pos))
            {
                if (connection.ToVoxel == voxelPos)
                {
                    return GetTerminal(connection.From, (byte3)connection.FromVoxel);
                }
            }

            return null;
        }

        public bool TryGetSettingOfType(ushort3 pos, int index, SettingType type, [MaybeNullWhen(false)] out object value)
        {
            if (Prefab.Settings.TryGetValue(pos, out var settings))
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

        public ImmutableArray<Connection> GetOutVoidConnections(ushort3 pos)
        {
            var builder = ImmutableArray.CreateBuilder<Connection>(1);

            foreach (var connection in Prefab.Connections)
            {
                if (connection.From != pos)
                {
                    continue;
                }

                ushort id = Prefab.Blocks.GetBlockOrDefault(connection.To);

                if (id == 0)
                {
                    continue;
                }

                var info = _globalCtx.PrefabInfos[id].TerminalInfo;

                bool isVoid = false;

                foreach (var terminal in info.InputTerminals)
                {
                    if (terminal.Position == connection.ToVoxel)
                    {
                        isVoid = terminal.Type == SignalType.Void;
                        break;
                    }
                }

                if (isVoid)
                {
                    builder.Add(connection);
                }
            }

            return builder.DrainToImmutable();
        }
    }

    private class ParsePrefabInfo
    {
        public ParsePrefabInfo(PrefabTerminalInfo terminalInfo, ParseContext parseCtx)
        {
            TerminalInfo = terminalInfo;
            ParseCtx = parseCtx;
        }

        public PrefabTerminalInfo TerminalInfo { get; }

        public ParseContext ParseCtx { get; }
    }
}