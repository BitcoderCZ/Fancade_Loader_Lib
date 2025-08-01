﻿using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Fancade.Runtime.Syntax;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Frozen;
using System.Collections.Immutable;
using static BitcoderCZ.Fancade.Utils.ThrowHelper;

namespace BitcoderCZ.Fancade.Runtime;

public sealed partial class AST
{
    public readonly ushort PrefabId;

    public readonly PrefabTerminalInfo TerminalInfo;

    public readonly ImmutableArray<(ushort3 BlockPosition, byte3 TerminalPosition)> NotConnectedVoidInputs;

    public readonly ImmutableArray<OutsideConnection> VoidInputs;
    public readonly ImmutableArray<(OutsideConnection Connection, SyntaxTerminal? InsideTerminal)> NonVoidOutputs;

    public readonly FrozenDictionary<ushort3, StatementSyntax> Statements;

    public readonly ImmutableArray<Variable> GlobalVariables;

    public readonly ImmutableArray<Variable> Variables;

    public readonly FrozenDictionary<ushort3, ImmutableArray<Connection>> ConnectionsFrom;

    public readonly FrozenDictionary<ushort3, ImmutableArray<Connection>> ConnectionsTo;

    public AST(ushort prefabId, PrefabTerminalInfo terminalInfo, ImmutableArray<(ushort3 BlockPosition, byte3 TerminalPosition)> notConnectedVoidInputs, FrozenDictionary<ushort3, StatementSyntax> statements, ImmutableArray<Variable> globalVariables, ImmutableArray<Variable> variables, ImmutableArray<OutsideConnection> voidInputs, ImmutableArray<(OutsideConnection Connection, SyntaxTerminal? InsideTerminal)> nonVoidOutputs, FrozenDictionary<ushort3, ImmutableArray<Connection>> connectionsFrom, FrozenDictionary<ushort3, ImmutableArray<Connection>> connectionsTo)
    {
        ThrowIfNull(notConnectedVoidInputs, nameof(notConnectedVoidInputs));
        ThrowIfNull(statements, nameof(statements));
        ThrowIfNull(globalVariables, nameof(globalVariables));
        ThrowIfNull(variables, nameof(variables));

        PrefabId = prefabId;
        TerminalInfo = terminalInfo;
        NotConnectedVoidInputs = notConnectedVoidInputs;
        Statements = statements;
        GlobalVariables = globalVariables;
        Variables = variables;
        VoidInputs = voidInputs;
        NonVoidOutputs = nonVoidOutputs;
        ConnectionsFrom = connectionsFrom;
        ConnectionsTo = connectionsTo;
    }

    /// <summary>
    /// Creates a new <see cref="AST"/> instance from a <see cref="PrefabList"/>.
    /// </summary>
    /// <param name="prefabs">
    /// The <see cref="PrefabList"/> to create the <see cref="AST"/> from.
    /// <para><see cref="PrefabListUtils.AddImplicitConnections(PrefabList, FrozenDictionary{ushort, PrefabTerminalInfo}?)"/> must be called before <see cref="Parse(PrefabList, ushort)"/>.</para>
    /// </param>
    /// <param name="mainPrefabId">Id of the "main" prefab (the open level).</param>
    /// <returns>The constructed <see cref="AST"/>.</returns>
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

    public readonly struct OutsideConnection
    {
        public readonly byte3 OutsidePosition;
        public readonly ushort3 BlockPosition;
        public readonly byte3 TerminalPosition;

        public OutsideConnection(byte3 outsidePosition, ushort3 blockPosition, byte3 terminalPosition)
        {
            OutsidePosition = outsidePosition;
            BlockPosition = blockPosition;
            TerminalPosition = terminalPosition;
        }
    }
}