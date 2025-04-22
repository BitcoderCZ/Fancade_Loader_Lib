using FancadeLoaderLib.Collections;
using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Raw;
using FancadeLoaderLib.Runtime.Syntax;
using FancadeLoaderLib.Runtime.Utils;
using MathUtils.Vectors;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

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

            return ctx.AST;
        }
    }

    private sealed class ParseContext
    {
        public readonly Prefab Prefab;

        internal readonly List<(ushort3 BlockPosition, byte3 TerminalPosition)> _notConnectedVoidInputs = [];

        internal readonly List<OutsideConnection> _voidInputs = [];
        internal readonly List<(OutsideConnection Connection, SyntaxTerminal? InsideTerminal)> _nonVoidOutputs = [];

        internal readonly Dictionary<ushort3, SyntaxNode> _nodes = [];

        internal readonly ImmutableArray<Variable> _variables;

        private readonly GlobalParseContext _globalCtx;

        private readonly bool _isDummy;

        private bool _parsed;

        private AST? _ast;

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

        public AST AST
        {
            get
            {
                if (_ast is not null)
                {
                    return _ast;
                }

                if (!_parsed && !_isDummy)
                {
                    ParseAll();
                }

                MultiValueDictionary<ushort3, Connection> connectionsFrom = [];
                MultiValueDictionary<ushort3, Connection> connectionsTo = [];

                foreach (var connection in Prefab.Connections)
                {
                    connectionsFrom.Add(connection.From, connection);
                    connectionsTo.Add(connection.To, connection);
                }

                return _ast = new AST(Prefab.Id, _globalCtx.PrefabInfos[Prefab.Id].TerminalInfo, [.. _notConnectedVoidInputs], _nodes.Where(item => item.Value is StatementSyntax).Select(item => new KeyValuePair<ushort3, StatementSyntax>(item.Key, (StatementSyntax)item.Value)).ToFrozenDictionary(), _globalCtx.GlobalVariables, _variables, [.. _voidInputs], [.. _nonVoidOutputs], connectionsFrom.ToFrozenDictionary(item => item.Key, item => item.Value.ToImmutableArray()), connectionsTo.ToFrozenDictionary(item => item.Key, item => item.Value.ToImmutableArray()));
            }
        }

        public bool Parsed => _parsed;

        public void ParseAll()
        {
            if (_parsed || _isDummy)
            {
                return;
            }

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

            foreach (var connection in Prefab.Connections)
            {
                if (connection.IsToOutside)
                {
                    ushort id = blocks.GetBlockOrDefault(connection.From);

                    if (id != 0)
                    {
                        var infos = _globalCtx.PrefabInfos[id].TerminalInfo;

                        bool isVoid = true;

                        foreach (var info in infos.OutputTerminals)
                        {
                            if (info.Position == connection.FromVoxel)
                            {
                                isVoid = info.Type == SignalType.Void;
                                break;
                            }
                        }

                        if (!isVoid)
                        {
                            _nonVoidOutputs.Add((new OutsideConnection((byte3)connection.ToVoxel, connection.From, (byte3)connection.FromVoxel), GetTerminal(connection.From, (byte3)connection.FromVoxel)));
                        }
                    }
                }
            }

            _parsed = true;
        }

        public bool TryCreateNode(ushort3 pos, [MaybeNullWhen(false)] out SyntaxNode node)
        {
            if (_nodes.ContainsKey(pos))
            {
                node = null;
                return false;
            }

            ushort id = Prefab.Blocks.GetBlockOrDefault(pos);

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
                                bool foundConnection = false;
                                foreach (var connection in GetConnectionsTo(Prefab.Connections, pos))
                                {
                                    if (connection.ToVoxel == termPos)
                                    {
                                        foundConnection = true;

                                        if (connection.IsFromOutside)
                                        {
                                            _voidInputs.Add(new OutsideConnection((byte3)connection.FromVoxel, pos, termPos));
                                        }
                                    }
                                }

                                if (!foundConnection)
                                {
                                    _notConnectedVoidInputs.Add((pos, termPos));
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
                    var infos = _globalCtx.PrefabInfos[id].TerminalInfo;

                    var connectedInputTerminals = ImmutableArray.CreateBuilder<(byte3 TerminalPosition, SyntaxTerminal? ConnectedTerminal)>(2);

                    foreach (var info in infos.InputTerminals)
                    {
                        if (info.Type != SignalType.Void)
                        {
                            connectedInputTerminals.Add((info.Position, GetConnectedTerminal(pos, info.Position)));
                        }
                    }

                    var customStatement = new CustomStatementSyntax(id, pos, GetOutVoidConnections(pos), _globalCtx.PrefabInfos[id].ParseCtx.AST, connectedInputTerminals.DrainToImmutable());
                    node = customStatement;

                    _nodes.Add(pos, node);

                    foreach (var termPos in customStatement.InputVoidTerminals)
                    {
                        bool foundConnection = false;
                        foreach (var connection in GetConnectionsTo(Prefab.Connections, pos))
                        {
                            if (connection.ToVoxel == termPos)
                            {
                                foundConnection = true;

                                if (connection.IsFromOutside)
                                {
                                    _voidInputs.Add(new OutsideConnection((byte3)connection.FromVoxel, pos, termPos));
                                }
                            }
                        }

                        if (!foundConnection)
                        {
                            _notConnectedVoidInputs.Add((pos, termPos));
                        }
                    }

                    return true;
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
                    if (connection.IsFromOutside)
                    {
                        return (SyntaxTerminal?)new SyntaxTerminal(new OuterExpressionSyntax(Prefab.Id, ushort3.One * Connection.IsFromToOutsideValue), (byte3)connection.FromVoxel);
                    }
                    else
                    {
                        var terminal = GetTerminal(connection.From, (byte3)connection.FromVoxel);

                        if (terminal is null)
                        {
                            ushort id = Prefab.Blocks.GetBlockOrDefault(connection.From);

                            if (id != 0)
                            {
                                terminal = new SyntaxTerminal(new ObjectExpressionSyntax(id, connection.From), (byte3)connection.FromVoxel);
                            }
                        }

                        return terminal;
                    }
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
                if (connection.From != pos || connection.IsFromOutside)
                {
                    continue;
                }

                bool isVoid = false;

                if (connection.IsToOutside)
                {
                    ushort id = Prefab.Blocks.GetBlockOrDefault(connection.From);

                    if (id == 0)
                    {
                        continue;
                    }

                    var info = _globalCtx.PrefabInfos[id].TerminalInfo;

                    foreach (var terminal in info.OutputTerminals)
                    {
                        if (terminal.Position == connection.FromVoxel)
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
                else
                {
                    ushort id = Prefab.Blocks.GetBlockOrDefault(connection.To);

                    if (id == 0)
                    {
                        continue;
                    }

                    var info = _globalCtx.PrefabInfos[id].TerminalInfo;

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
            }

            builder.Sort((a, b) => ScriptPositionComparer.Instance.Compare(a.To, b.To));

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