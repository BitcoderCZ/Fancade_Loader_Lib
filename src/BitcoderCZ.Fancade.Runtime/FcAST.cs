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
    /// <summary>
    /// Initializes a new instance of the <see cref="FcAST"/> class.
    /// </summary>
    /// <param name="prefabId">Id of the prefab this AST represents.</param>
    /// <param name="terminalInfo"><see cref="PrefabTerminalInfo"/> for the prefab.</param>
    /// <param name="entryPointTerminals">Positions of input void terminals, that are not connected.</param>
    /// <param name="statements">Map of positions to statements.</param>
    /// <param name="globalVariables">All global variables.</param>
    /// <param name="variables">Local variable of the <see cref="FcAST"/>.</param>
    /// <param name="voidInputs">Void inputs to the <see cref="FcAST"/>.</param>
    /// <param name="nonVoidOutputs">Inputs of type other than <see cref="SignalType.Void"/> of the <see cref="FcAST"/>.</param>
    /// <param name="connectionsFrom">A map of connections by the block they originate from.</param>
    /// <param name="connectionsTo">A map of connections by the block they end on.</param>
    public FcAST(ushort prefabId, PrefabTerminalInfo terminalInfo, ImmutableArray<(ushort3 BlockPosition, byte3 TerminalPosition)> entryPointTerminals, FrozenDictionary<ushort3, StatementSyntax> statements, ImmutableArray<Variable> globalVariables, ImmutableArray<Variable> variables, ImmutableArray<OutsideConnection> voidInputs, ImmutableArray<(OutsideConnection Connection, SyntaxTerminal? InsideTerminal)> nonVoidOutputs, FrozenDictionary<ushort3, ImmutableArray<Connection>> connectionsFrom, FrozenDictionary<ushort3, ImmutableArray<Connection>> connectionsTo)
    {
        ThrowIfNull(statements);
        ThrowIfNull(connectionsFrom);
        ThrowIfNull(connectionsTo);

        PrefabId = prefabId;
        TerminalInfo = terminalInfo;
        EntryPointTerminals = entryPointTerminals;
        Statements = statements;
        GlobalVariables = globalVariables;
        Variables = variables;
        VoidInputs = voidInputs;
        NonVoidOutputs = nonVoidOutputs;
        ConnectionsFrom = connectionsFrom;
        ConnectionsTo = connectionsTo;
    }

    /// <summary>
    /// Gets the id of the prefab this AST represents.
    /// </summary>
    /// <value>Id of the prefab this AST represents.</value>
    public ushort PrefabId { get; }

    /// <summary>
    /// Gets the <see cref="PrefabTerminalInfo"/> for the prefab.
    /// </summary>
    /// <value><see cref="PrefabTerminalInfo"/> for the prefab.</value>
    public PrefabTerminalInfo TerminalInfo { get; }

    /// <summary>
    /// Gets the positions of input void terminals, that are not connected.
    /// </summary>
    /// <value>Positions of input void terminals, that are not connected.</value>
    public ImmutableArray<(ushort3 BlockPosition, byte3 TerminalPosition)> EntryPointTerminals { get; }

    /// <summary>
    /// Gets a map of positions to statements.
    /// </summary>
    /// <value>Map of positions to statements.</value>
    public FrozenDictionary<ushort3, StatementSyntax> Statements { get; }

    /// <summary>
    /// Gets all global variables.
    /// </summary>
    /// <value>All global variables.</value>
    public ImmutableArray<Variable> GlobalVariables { get; }

    /// <summary>
    /// Gets the local variable of the <see cref="FcAST"/>.
    /// </summary>
    /// <value>Local variable of the <see cref="FcAST"/>.</value>
    public ImmutableArray<Variable> Variables { get; }

    /// <summary>
    /// Gets the void inputs to the <see cref="FcAST"/>.
    /// </summary>
    /// <value>Void inputs to the <see cref="FcAST"/>.</value>
    public ImmutableArray<OutsideConnection> VoidInputs { get; }

    /// <summary>
    /// Gets the inputs of type other than <see cref="SignalType.Void"/> of the <see cref="FcAST"/>.
    /// </summary>
    /// <value>Inputs of type other than <see cref="SignalType.Void"/> of the <see cref="FcAST"/>.</value>
    public ImmutableArray<(OutsideConnection Connection, SyntaxTerminal? InsideTerminal)> NonVoidOutputs { get; }

    /// <summary>
    /// Gets a map of connections by the block they originate from.
    /// </summary>
    /// <value>A map of connections by the block they originate from.</value>
    public FrozenDictionary<ushort3, ImmutableArray<Connection>> ConnectionsFrom { get; }

    /// <summary>
    /// Gets a map of connections by the block they end on.
    /// </summary>
    /// <value>A map of connections by the block they end on.</value>
    public FrozenDictionary<ushort3, ImmutableArray<Connection>> ConnectionsTo { get; }

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