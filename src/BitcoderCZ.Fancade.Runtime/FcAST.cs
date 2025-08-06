using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Fancade.Runtime.Syntax;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Frozen;
using System.Collections.Immutable;
using static BitcoderCZ.Fancade.Utils.ThrowHelper;

namespace BitcoderCZ.Fancade.Runtime;

/// <summary>
/// Represents the abstract syntax tree (AST) of a Fancade prefab.
/// </summary>
public sealed partial class FcAST
{
    public readonly ImmutableArray<(ushort3 BlockPosition, byte3 TerminalPosition)> NotConnectedVoidInputs;

    public readonly ImmutableArray<OutsideConnection> VoidInputs;
    public readonly ImmutableArray<(OutsideConnection Connection, SyntaxTerminal? InsideTerminal)> NonVoidOutputs;

    public readonly FrozenDictionary<ushort3, StatementSyntax> Statements;

    public readonly ImmutableArray<Variable> GlobalVariables;

    public readonly ImmutableArray<Variable> Variables;

    public readonly FrozenDictionary<ushort3, ImmutableArray<Connection>> ConnectionsFrom;

    public readonly FrozenDictionary<ushort3, ImmutableArray<Connection>> ConnectionsTo;

    /// <summary>
    /// Initializes a new instance of the <see cref="FcAST"/> class.
    /// </summary>
    /// <param name="prefabId">The prefab this AST represents.</param>
    /// <param name="terminalInfo"><see cref="PrefabTerminalInfo"/> for the prefab.</param>
    /// <param name="notConnectedVoidInputs"></param>
    /// <param name="statements"></param>
    /// <param name="globalVariables"></param>
    /// <param name="variables"></param>
    /// <param name="voidInputs"></param>
    /// <param name="nonVoidOutputs"></param>
    /// <param name="connectionsFrom"></param>
    /// <param name="connectionsTo"></param>
    public FcAST(ushort prefabId, PrefabTerminalInfo terminalInfo, ImmutableArray<(ushort3 BlockPosition, byte3 TerminalPosition)> notConnectedVoidInputs, FrozenDictionary<ushort3, StatementSyntax> statements, ImmutableArray<Variable> globalVariables, ImmutableArray<Variable> variables, ImmutableArray<OutsideConnection> voidInputs, ImmutableArray<(OutsideConnection Connection, SyntaxTerminal? InsideTerminal)> nonVoidOutputs, FrozenDictionary<ushort3, ImmutableArray<Connection>> connectionsFrom, FrozenDictionary<ushort3, ImmutableArray<Connection>> connectionsTo)
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

    public ushort PrefabId { get; }

    public PrefabTerminalInfo TerminalInfo { get; }

    /// <summary>
    /// Creates a new <see cref="FcAST"/> instance from a <see cref="PrefabList"/>.
    /// </summary>
    /// <param name="prefabs">
    /// The <see cref="PrefabList"/> to create the <see cref="FcAST"/> from.
    /// <para><see cref="PrefabListUtils.AddImplicitConnections(PrefabList, FrozenDictionary{ushort, PrefabTerminalInfo}?)"/> must be called before <see cref="Parse(PrefabList, ushort)"/>.</para>
    /// </param>
    /// <param name="mainPrefabId">Id of the "main" prefab (the open level).</param>
    /// <returns>The constructed <see cref="FcAST"/>.</returns>
    public static FcAST Parse(PrefabList prefabs, ushort mainPrefabId)
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

    /// <summary>
    /// Represents a connection between the inside and the outside of a prefab.
    /// </summary>
    public readonly struct OutsideConnection
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OutsideConnection"/> struct.
        /// </summary>
        /// <param name="outsideTerminal">Position of the outside terminal.</param>
        /// <param name="insideBlock">Position of the block inside the prefab.</param>
        /// <param name="insideTerminal">Position of the inside terminal.</param>
        public OutsideConnection(byte3 outsideTerminal, ushort3 insideBlock, byte3 insideTerminal)
        {
            OutsideTerminal = outsideTerminal;
            InsideBlock = insideBlock;
            InsideTerminal = insideTerminal;
        }

        /// <summary>
        /// Gets the position of the outside terminal.
        /// </summary>
        /// <value>Position of the outside terminal.</value>
        public readonly byte3 OutsideTerminal { get; }

        /// <summary>
        /// Gets the position of the block inside the prefab.
        /// </summary>
        /// <value>Position of the block inside the prefab.</value>
        public readonly ushort3 InsideBlock { get; }

        /// <summary>
        /// Gets the position of the inside terminal.
        /// </summary>
        /// <value>Position of the inside terminal.</value>
        public readonly byte3 InsideTerminal { get; }
    }
}