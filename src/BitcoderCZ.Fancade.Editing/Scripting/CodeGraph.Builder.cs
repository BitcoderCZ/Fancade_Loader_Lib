// <copyright file="CodeGraph.Builder.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using BitcoderCZ.Maths.Vectors;
using static BitcoderCZ.Utils.ThrowHelper;

namespace BitcoderCZ.Fancade.Editing.Scripting;

public sealed partial class CodeGraph
{
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
            : this(32)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Builder"/> class.
        /// Starts with a root statement scope.
        /// </summary>
        /// <param name="initialCapacity">The initial node capacity.</param>
        public Builder(int initialCapacity)
        {
            Clear(initialCapacity);
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
            => Clear(0);

        internal static bool RemoveEmptyScopes(CodeScope scope)
        {
            for (int i = scope._children.Count - 1; i >= 0; i--)
            {
                if (RemoveEmptyScopes(scope._children[i]))
                {
                    var child = scope._children[i];
                    scope._children.RemoveAt(i);
                    if (child._children.Count > 0)
                    {
                        foreach (var childChild in child._children)
                        {
                            childChild.DeclaringNodeIndex = child.DeclaringNodeIndex;
                        }

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

        [MemberNotNull(nameof(_nodes), nameof(_regions), nameof(_connections))]
        private void Clear(int initialCapacity)
        {
            _scopeStack.Clear();
            _scopeStack.Push(new CodeScope(ScopeType.Statement)); // root scope

#pragma warning disable IDE0028 // Simplify collection initialization
            _nodes = new(initialCapacity);
            _regions = new(0);
            _connections = new(initialCapacity);
#pragma warning restore IDE0028 // Simplify collection initialization
            _graphId = GetNextGraphId();
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
