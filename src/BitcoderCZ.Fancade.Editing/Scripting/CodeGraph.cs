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
    public static CodeGraph Empty { get; } = new CodeGraph(new(ScopeType.Statement), [], [], 0);

    internal readonly List<Node.Connection> _connections;
    internal readonly List<Node.BlockRegion> _regions;

    internal CodeGraph(CodeScope rootScope, List<Node.BlockRegion> regions, List<Node.Connection> connections, int noedCount)
    {
        Debug.Assert(rootScope is not null);
        Debug.Assert(connections is not null);

        RootScope = rootScope;
        _regions = regions;
        _connections = connections;
        NodeCount = noedCount;
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
    /// Gets the connections between nodes in the graph.
    /// </summary>
    /// <value>Connections between nodes in the graph.</value>
    public IReadOnlyList<Node.Connection> Connections => _connections;

    /// <summary>
    /// Gets the total number of nodes in the graph.
    /// </summary>
    /// <value>Total number of nodes in the graph.</value>
    public int NodeCount { get; }

    /// <summary>
    /// Builder for creating a <see cref="CodeGraph"/>.
    /// </summary>
    public sealed class Builder
    {
        private readonly Stack<CodeScope> _scopeStack = new(8);
        private List<Node.BlockRegion> _regions = [];
        private List<Node.Connection> _connections = [];
        private int _nextNodeId = 0;
        private int _nextRegionId = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="Builder"/> class.
        /// Starts with a root statement scope.
        /// </summary>
        public Builder()
        {
            _scopeStack.Push(new CodeScope(ScopeType.Statement)); // root scope
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
        /// Places a new node of the specified type in the current scope.
        /// </summary>
        /// <param name="type">The block definition type to place.</param>
        /// <returns>The newly created <see cref="Node"/>.</returns>
        public Node Place(BlockDef type)
        {
            var node = new Node(type, _nextNodeId++);

            _scopeStack.Peek()._nodes.Add(node);

            return node;
        }

        /// <summary>
        /// Places an empty node.
        /// </summary>
        public void PlaceEmptyNode()
        {
            _nextNodeId++;
            _scopeStack.Peek()._nodes.Add(Node.Empty);
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
            var region = new Node.BlockRegion(array, _nextRegionId++);
            _regions.Add(region);

            return region;
        }

#pragma warning disable CA1822 // Mark members as static - don't expose implementation details
        /// <summary>
        /// Adds a setting to a node.
        /// </summary>
        /// <param name="node">The node to set.</param>
        /// <param name="setting">The setting to add.</param>
        public void SetSetting(Node node, PrefabSetting setting)
#pragma warning restore CA1822 // Mark members as static
            => node._settings.Add(setting);

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
            else if (topScope._nodes[^1] == Node.Empty)
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
            else if (topScope._nodes[^1] == Node.Empty)
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

            _scopeStack.Clear();
            _scopeStack.Push(new CodeScope(ScopeType.Statement)); // root scope

            _ = RemoveEmptyScopes(rootScope);

            var regions = _regions;
            var connections = _connections;
            int nodeCount = _nextNodeId;

            _regions = [];
            _connections = [];
            _nextNodeId = 0;

            return new CodeGraph(rootScope, regions, connections, nodeCount);
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
                while (lastExpressionChild._nodes.Last() == Node.Empty)
                {
                    lastExpressionChild._nodes.RemoveAt(lastExpressionChild._nodes.Count - 1);
                }
            }

            return !scope._nodes.Any(static node => node != Node.Empty);
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
    internal readonly List<Node> _nodes = [];
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
    public IReadOnlyList<Node> Nodes => _nodes;

    /// <summary>
    /// Gets the child scopes contained in this scope.
    /// </summary>
    /// <value>Child scopes contained in this scope.</value>
    public IReadOnlyList<CodeScope> Children => _children;

    internal int3? FirstNodeSize => _nodes.Count > 0 ? _nodes[0].Type.Size : null;

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
