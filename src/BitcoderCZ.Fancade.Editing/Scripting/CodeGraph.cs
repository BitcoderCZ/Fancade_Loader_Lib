// <copyright file="CodeGraph.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BitcoderCZ.Buffers;
using BitcoderCZ.Maths.Vectors;
using BitcoderCZ.Utils;
using static BitcoderCZ.Utils.ThrowHelper;
using SettingsCollection = BitcoderCZ.Buffers.InlineList<BitcoderCZ.Buffers.FixedArray2<BitcoderCZ.Fancade.PrefabSetting>, BitcoderCZ.Fancade.PrefabSetting>;
using TerminalsOutBuffer = BitcoderCZ.Buffers.FixedArray2<BitcoderCZ.Fancade.Editing.Scripting.Node.Terminal>;
using TerminalsOutCollection = BitcoderCZ.Buffers.ImmutableInlineArray<BitcoderCZ.Buffers.FixedArray2<BitcoderCZ.Fancade.Editing.Scripting.Node.Terminal>, BitcoderCZ.Fancade.Editing.Scripting.Node.Terminal>;

namespace BitcoderCZ.Fancade.Editing.Scripting;

/// <summary>
/// Represents a structured graph of code nodes and connections.
/// </summary>
public sealed class CodeGraph
{
    /// <summary>
    /// Gets an empty <see cref="CodeGraph"/> instance.
    /// </summary>
    /// <value>An empty <see cref="CodeGraph"/> instance.</value>
    public static CodeGraph Empty { get; } = new CodeGraph(0, [], new(ScopeType.Statement), [], []);

    internal readonly ushort _id;
    internal readonly List<NodeData> _nodes;
    internal readonly List<Node.Connection> _connections;
    internal readonly List<Node.BlockRegion> _regions;

    private static int nextGraphId = 0;

    internal CodeGraph(ushort id, List<NodeData> nodes, CodeScope rootScope, List<Node.BlockRegion> regions, List<Node.Connection> connections)
    {
        Debug.Assert(nodes is not null);
        Debug.Assert(rootScope is not null);
        Debug.Assert(connections is not null);

        _id = id;
        _nodes = nodes;
        RootScope = rootScope;
        _regions = regions;
        _connections = connections;
    }

    /// <summary>
    /// Gets the root scope of the <see cref="CodeGraph"/>.
    /// </summary>
    /// <value>The root scope of the <see cref="CodeGraph"/>.</value>
    public CodeScope RootScope { get; }

    /// <summary>
    /// Gets the <see cref="Node.BlockRegion"/>s in the <see cref="CodeGraph"/>.
    /// </summary>
    /// <value>The <see cref="Node.BlockRegion"/>s in the <see cref="CodeGraph"/>.</value>
    public IReadOnlyList<Node.BlockRegion> Regions => _regions;

    /// <summary>
    /// Gets the <see cref="Node.BlockRegion"/>s in the <see cref="CodeGraph"/> as a span.
    /// </summary>
    /// <value>The <see cref="Node.BlockRegion"/>s in the <see cref="CodeGraph"/> as a span.</value>
    public ReadOnlySpan<Node.BlockRegion> RegionsSpan => CollectionsMarshal.AsSpan(_regions);

    /// <summary>
    /// Gets the connections between nodes in the graph.
    /// </summary>
    /// <value>Connections between nodes in the graph.</value>
    public IReadOnlyList<Node.Connection> Connections => _connections;

    /// <summary>
    /// Gets the connections between nodes in the graph as a span.
    /// </summary>
    /// <value>Connections between nodes in the graph as a span.</value>
    public ReadOnlySpan<Node.Connection> ConnectionsSpan => CollectionsMarshal.AsSpan(_connections);

    /// <summary>
    /// Gets the total number of nodes in the graph.
    /// </summary>
    /// <value>Total number of nodes in the graph.</value>
    public int NodeCount => _nodes.Count;

    /// <summary>
    /// Gets a node from the graph.
    /// </summary>
    /// <param name="index">Index of the node to get.</param>
    /// <param name="settings">Settings of the node.</param>
    /// <returns>The node.</returns>
    public Node GetNode(int index, out NodeSettingsCollection settings)
    {
        var data = _nodes[index];
        if (data.Type is null)
        {
            settings = default;
            return Node.Empty;
        }

        settings = new NodeSettingsCollection(data.Settings);
        return new Node(_id, (ushort)index, data.Type);
    }

    /// <summary>
    /// Gets a node from the graph.
    /// </summary>
    /// <param name="handle">Handle of the node to get.</param>
    /// <param name="settings">Settings of the node.</param>
    /// <returns>The node.</returns>
    public Node GetNode(NodeHandle handle, out NodeSettingsCollection settings)
    {
        if (handle._graphId != _id)
        {
            ThrowArgumentException($"{nameof(handle)} belongs to another {nameof(CodeGraph)}.", nameof(handle));
        }

        var data = _nodes[handle._index];
        if (data.Type is null)
        {
            settings = default;
            return Node.Empty;
        }

        settings = new NodeSettingsCollection(data.Settings);
        return new Node(_id, handle._index, data.Type);
    }

    private static ushort GetNextGraphId()
        => (ushort)Interlocked.Increment(ref nextGraphId);

    /// <summary>
    /// Builder for creating a <see cref="CodeGraph"/>.
    /// </summary>
    public sealed class Builder
    {
        private readonly Stack<CodeScope> _scopeStack = new(8);
        private ushort _graphId;
        private List<NodeData> _nodes;
        private List<Node.BlockRegion> _regions;
        private List<Node.Connection> _connections;

        /// <summary>
        /// Initializes a new instance of the <see cref="Builder"/> class.
        /// Starts with a root statement scope.
        /// </summary>
        public Builder()
        {
            Clear();
        }

        /// <summary>
        /// Gets the type of the currently active scope.
        /// </summary>
        /// <value>Type of the currently active scope.</value>
        public ScopeType CurrentScopeType => _scopeStack.Peek().Type;

        /// <summary>
        /// Gets the amount of nodes in the currently active scope.
        /// </summary>
        /// <value>Amount of nodes in the currently active scope.</value>
        public int CurrentScopeNodeCount => _scopeStack.Peek()._nodes.Count;

        /// <summary>
        /// Gets the amount of nodes in the root scope.
        /// </summary>
        /// <value>Amount of nodes in the root scope.</value>
        public int RootScopeNodeCount => _scopeStack.Last()._nodes.Count;

        /// <summary>
        /// Places a new node of the specified type in the current scope.
        /// </summary>
        /// <param name="type">The block definition type to place.</param>
        /// <returns>The newly created <see cref="Node"/>.</returns>
        public Node Place(BlockDef type)
        {
            var node = new Node(_graphId, (ushort)_nodes.Count, type);
            _nodes.Add(new NodeData(type, default));

            _scopeStack.Peek()._nodes.Add(node.Handle);

            return node;
        }

        /// <summary>
        /// Places an empty node in the currently active scope.
        /// </summary>
        public void PlaceEmptyNode()
        {
            _nodes.Add(default);
            _scopeStack.Peek()._nodes.Add(NodeHandle.Null);
        }

        /// <summary>
        /// Creates a <see cref="Node.BlockRegion"/>, for placing non script blocks.
        /// </summary>
        /// <remarks>
        /// Use <see cref="Node.Terminal.ObjectRelative(Node.BlockRegion, int3, byte3)"/> to reference blocks inside the <see cref="Node.BlockRegion"/>.
        /// </remarks>
        /// <param name="size">Size of the region to create.</param>
        /// <returns>The created <see cref="Node.BlockRegion"/>.</returns>
        public Node.BlockRegion CreateRegion(int3 size)
        {
            var array = new Array3D<ushort>(size);
            var region = new Node.BlockRegion(_graphId, (ushort)_regions.Count, array);
            _regions.Add(region);

            return region;
        }

        /// <summary>
        /// Adds a setting to a node.
        /// </summary>
        /// <param name="node">The node to set.</param>
        /// <param name="setting">The setting to add.</param>
        public void SetSetting(Node node, PrefabSetting setting)
            => SetSetting(node.Handle, setting);

        /// <summary>
        /// Adds a setting to a node.
        /// </summary>
        /// <param name="handle">The node to set.</param>
        /// <param name="setting">The setting to add.</param>
        public void SetSetting(NodeHandle handle, PrefabSetting setting)
        {
            if (handle._graphId != _graphId)
            {
                ThrowArgumentException($"{nameof(handle)} belongs to another {nameof(CodeGraph)}.", nameof(handle));
            }

            // need ref to item
            CollectionsMarshal.AsSpan(_nodes)[handle._index].Settings.Add(setting);
        }

        /// <summary>
        /// Connects a <see cref="Node.Terminal"/> to a <see cref="Node.Terminal"/>.
        /// </summary>
        /// <remarks>
        /// Ignores connections if either terminal is null (<see cref="Node.Terminal.IsNull"/>).
        /// </remarks>
        /// <param name="from">The source <see cref="Node.Terminal"/>.</param>
        /// <param name="to">The target <see cref="Node.Terminal"/>.</param>
        public void Connect(Node.Terminal from, Node.Terminal to)
        {
            if (from.IsNull || to.IsNull)
            {
                return;
            }

            _connections.Add(new Node.Connection(from, to));
        }

        /// <summary>
        /// Enters a new statement scope.
        /// </summary>
        /// <remarks>
        /// A statement scope cannot be enter when inside of an expression scope.
        /// </remarks>
        /// <returns>A disposable that exits the scope when disposed.</returns>
        public StatementScopeDisposable StatementScope()
            => new StatementScopeDisposable(this);

        /// <summary>
        /// Enters a new expression scope.
        /// </summary>
        /// <returns>A disposable that exits the scope when disposed.</returns>
        public ExpressionScopeDisposable ExpressionScope()
            => new ExpressionScopeDisposable(this);

        /// <summary>
        /// Enters a new statement scope.
        /// </summary>
        /// <remarks>
        /// A statement scope cannot be enter when inside of an expression scope.
        /// </remarks>
        public void EnterStatementScope()
        {
            var topScope = _scopeStack.Peek();
            if (topScope.Type is ScopeType.Expression)
            {
                ThrowInvalidOperationException("Cannot enter a statement scope while in an expression scope.");
            }
            else if (topScope._nodes.Count is 0)
            {
                ThrowInvalidOperationException("Cannot enter new scope when the current scope has no node.");
            }
            else if (topScope._nodes[^1] == default)
            {
                ThrowInvalidOperationException("Cannot enter new scope when the current scope's last node is an empty node'.");
            }

            var newScope = new CodeScope(ScopeType.Statement, topScope);
            topScope._children.Add(newScope);
            _scopeStack.Push(newScope);
        }

        /// <summary>
        /// Exits the current statement scope.
        /// </summary>
        public void ExitStatementScope()
        {
            if (_scopeStack.Count <= 1)
            {
                ThrowInvalidOperationException("Cannot exit root scope");
            }

            var topScope = _scopeStack.Peek();
            if (topScope.Type is ScopeType.Expression)
            {
                ThrowInvalidOperationException("Attempted to exit a statement scope while inside an expression scope.");
            }

            _ = _scopeStack.Pop();
            Debug.Assert(_scopeStack.Count > 0);
        }

        /// <summary>
        /// Enters a new expression scope.
        /// </summary>
        public void EnterExpressionScope()
        {
            var topScope = _scopeStack.Peek();

            if (topScope._nodes.Count is 0)
            {
                ThrowInvalidOperationException("Cannot enter new scope when the current scope has no nodes.");
            }
            else if (topScope._nodes[^1] == default)
            {
                ThrowInvalidOperationException("Cannot enter new scope when the current scope's last node is an empty node'.");
            }

            var newScope = new CodeScope(ScopeType.Expression, topScope);
            topScope._children.Add(newScope);
            _scopeStack.Push(newScope);
        }

        /// <summary>
        /// Exits the current expression scope.
        /// </summary>
        public void ExitExpressionScope()
        {
            var topScope = _scopeStack.Peek();
            if (topScope.Type is ScopeType.Statement)
            {
                ThrowInvalidOperationException("Attempted to exit an expression scope while inside a statement scope.");
            }

            _ = _scopeStack.Pop();
            Debug.Assert(_scopeStack.Count > 0);
        }

        /// <summary>
        /// Builds the final <see cref="CodeGraph"/> and clears the builder for reuse.
        /// </summary>
        /// <returns>The constructed <see cref="CodeGraph"/>.</returns>
        public CodeGraph BuildAndClear()
        {
            var rootScope = _scopeStack.Last();
            Debug.Assert(rootScope.Type is ScopeType.Statement);

            _ = RemoveEmptyScopes(rootScope);

            var graphId = _graphId;
            var nodes = _nodes;
            var regions = _regions;
            var connections = _connections;

            Clear();

            return new CodeGraph(graphId, nodes, rootScope, regions, connections);
        }

        /// <summary>
        /// Clears the contents of the builder.
        /// </summary>
        [MemberNotNull(nameof(_nodes), nameof(_regions), nameof(_connections))]
        public void Clear()
        {
            _scopeStack.Clear();
            _scopeStack.Push(new CodeScope(ScopeType.Statement)); // root scope

            _nodes = [];
            _regions = [];
            _connections = [];
            _graphId = GetNextGraphId();
        }

        private static bool RemoveEmptyScopes(CodeScope scope)
        {
            for (int i = scope._children.Count - 1; i >= 0; i--)
            {
                if (RemoveEmptyScopes(scope._children[i]))
                {
                    var child = scope._children[i];
                    scope._children.RemoveAt(i);
                    if (child._children.Count > 0)
                    {
                        scope._children.InsertRange(i, child._children);
                    }
                }
            }

            var lastExpressionChild = scope._children.LastOrDefault(static child => child.Type is ScopeType.Expression);
            if (lastExpressionChild is not null)
            {
                while (lastExpressionChild._nodes.Last() == default)
                {
                    lastExpressionChild._nodes.RemoveAt(lastExpressionChild._nodes.Count - 1);
                }
            }

            return !scope._nodes.Any(static node => node != default);
        }

        /// <summary>
        /// Disposable helper to automatically exit a statement scope.
        /// </summary>
        public struct StatementScopeDisposable : IDisposable
        {
            private Builder? _builder;

            internal StatementScopeDisposable(Builder builder)
            {
                _builder = builder;
                _builder.EnterStatementScope();
            }

            /// <summary>
            /// Exits the statement scope.
            /// </summary>
            public void Dispose()
            {
                // todo: should this validate that it's existing the same scope it entered?
                _builder?.ExitStatementScope();
                _builder = null;
            }
        }

        /// <summary>
        /// Disposable helper to automatically exit an expression scope.
        /// </summary>
        public struct ExpressionScopeDisposable : IDisposable
        {
            private Builder? _builder;

            internal ExpressionScopeDisposable(Builder builder)
            {
                _builder = builder;
                _builder.EnterExpressionScope();
            }

            /// <summary>
            /// Exits the expression scope.
            /// </summary>
            public void Dispose()
            {
                // todo: should this validate that it's existing the same scope it entered?
                _builder?.ExitExpressionScope();
                _builder = null;
            }
        }
    }
}

/// <summary>
/// Specifies the type of a code scope.
/// </summary>
public enum ScopeType
{
    /// <summary>
    /// A scope containing statements.
    /// </summary>
    Statement,

    /// <summary>
    /// A scope containing expressions.
    /// </summary>
    Expression,
}

/// <summary>
/// Represents a scope in a <see cref="CodeGraph"/> that contains nodes and child scopes.
/// </summary>
public sealed class CodeScope
{
    internal readonly List<NodeHandle> _nodes = [];
    internal readonly List<CodeScope> _children = [];
    private int? _maxExpressionDepth;
    private int? _firstExpressionDepth;

    internal CodeScope(ScopeType type)
    {
        Type = type;
    }

    internal CodeScope(ScopeType type, CodeScope parent)
    {
        Type = type;
        Parent = parent;
        DeclaringNodeIndex = parent._nodes.Count - 1;
        Debug.Assert(DeclaringNodeIndex >= 0);
    }

    /// <summary>
    /// Gets the type of this scope.
    /// </summary>
    /// <value>Type of this scope.</value>
    public ScopeType Type { get; }

    /// <summary>
    /// Gets the parent scope.
    /// </summary>
    /// <value>Parent scope; or <see langword="null"/>, if this is the root scope.</value>
    public CodeScope? Parent { get; }

    /// <summary>
    /// Gets the index of the node in the parent scope that declares this scope.
    /// </summary>
    /// <remarks>
    /// The declaring node is the node that was active when this scope was created and is considered the logical owner of the scope.
    /// </remarks>
    /// <value>
    /// The index of the declaring node in <see cref="Parent"/>'s <see cref="Nodes"/> collection; or <see langword="null"/>, if this is the root scope (<see cref="Parent"/> is <see langword="null"/>).
    /// </value>
    public int? DeclaringNodeIndex { get; }

    /// <summary>
    /// Gets the nodes directly contained in this scope.
    /// </summary>
    /// <value>Nodes directly contained in the scope.</value>
    public IReadOnlyList<NodeHandle> Nodes => _nodes;

    /// <summary>
    /// Gets the child scopes contained in this scope.
    /// </summary>
    /// <value>Child scopes contained in this scope.</value>
    public IReadOnlyList<CodeScope> Children => _children;

    internal int GetMaxExpressionDepth()
    {
        if (_maxExpressionDepth is { } maxExpressionDepth)
        {
            return maxExpressionDepth;
        }

        maxExpressionDepth = 0;

        foreach (var child in _children)
        {
            if (child.Type is not ScopeType.Expression)
            {
                continue;
            }

            maxExpressionDepth = Math.Max(maxExpressionDepth, 1 + child.GetMaxExpressionDepth());
        }

        _maxExpressionDepth = maxExpressionDepth;
        return maxExpressionDepth;
    }

    internal int GetFirstExpressionDepth()
    {
        if (_firstExpressionDepth is { } firstExpressionDepth)
        {
            return firstExpressionDepth;
        }

        var firstExpression = _children.FirstOrDefault(static child => child.Type is ScopeType.Expression);
        if (firstExpression is null)
        {
            firstExpressionDepth = 0;
        }
        else
        {
            firstExpressionDepth = 1 + firstExpression.GetFirstExpressionDepth();
        }

        _firstExpressionDepth = firstExpressionDepth;
        return firstExpressionDepth;
    }

    internal int GetHorizontalSize()
    {
        int expressionDepth = GetMaxExpressionDepth();

        int statementDepth = 0;

        foreach (var child in _children)
        {
            if (child.Type is not ScopeType.Statement)
            {
                continue;
            }

            statementDepth = Math.Max(statementDepth, child.GetHorizontalSize());
        }

        return expressionDepth + 1 + statementDepth;
    }
}
