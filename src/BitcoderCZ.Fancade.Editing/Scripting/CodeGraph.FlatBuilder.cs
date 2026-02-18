// <copyright file="CodeGraph.FlatBuilder.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BitcoderCZ.Maths.Vectors;
using static BitcoderCZ.Utils.ThrowHelper;
using SettingsCollection = BitcoderCZ.Buffers.InlineList<BitcoderCZ.Buffers.FixedArray1<BitcoderCZ.Fancade.PrefabSetting>, BitcoderCZ.Fancade.PrefabSetting>;

namespace BitcoderCZ.Fancade.Editing.Scripting;

public sealed partial class CodeGraph
{
    /// <summary>
    /// Builder for creating a <see cref="CodeGraph"/>, without statement scopes.
    /// </summary>
    public sealed class FlatBuilder
    {
        internal ushort _graphId;

        private readonly List<Node> _nodes;

        private Stack<CodeScope>? _cachedScopeStack;
        private List<Scripting.Node.BlockRegion> _regions;
        private List<Scripting.Node.Connection> _connections;
        private int _expressionDepth;

        private Dictionary<ushort, (int NodeIndex, int RegionIndex)>? _mergedBuilderOffsets;

        /// <summary>
        /// Initializes a new instance of the <see cref="FlatBuilder"/> class.
        /// </summary>
        public FlatBuilder()
            : this(32)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FlatBuilder"/> class.
        /// </summary>
        /// <param name="initialCapacity">Initial node capacity.</param>
        public FlatBuilder(int initialCapacity)
        {
#pragma warning disable IDE0028 // Simplify collection initialization
            _nodes = new(initialCapacity);
#pragma warning restore IDE0028 // Simplify collection initialization
            Clear(initialCapacity);
        }

        /// <summary>
        /// Gets the number of nodes contained in the builder.
        /// </summary>
        /// <value>Number of nodes contained in the builder.</value>
        public int NodeCount => _nodes.Count;

        /// <summary>
        /// Gets the current expression scope depth.
        /// </summary>
        /// <value>The current expression scope depth.</value>
        public int CurrentExpressionDepth => _expressionDepth;

        private Span<Node> NodesSpan => CollectionsMarshal.AsSpan(_nodes);

        private Span<Scripting.Node.BlockRegion> RegionsSpan => CollectionsMarshal.AsSpan(_regions);

        private Span<Scripting.Node.Connection> ConnectionsSpan => CollectionsMarshal.AsSpan(_connections);

        /// <summary>
        /// Places a new node of the specified type in the current scope.
        /// </summary>
        /// <param name="type">The block definition type to place.</param>
        /// <returns>The newly created <see cref="Node"/>.</returns>
        public ref Node Place(BlockDef type)
        {
            ref var node = ref AddNode();
            node = new Node(new(_graphId, (ushort)(_nodes.Count - 1)), type, _expressionDepth);

            return ref node;
        }

        /// <summary>
        /// Places an empty node in the currently active scope.
        /// </summary>
        /// <returns>The newly created <see cref="Node"/>.</returns>
        public ref Node PlaceEmptyNode()
        {
            ref var node = ref AddNode();
            node = new(NodeHandle.Null, null, _expressionDepth);
            return ref node;
        }

        /// <summary>
        /// Creates a <see cref="Scripting.Node.BlockRegion"/>, for placing non script blocks.
        /// </summary>
        /// <remarks>
        /// Use <see cref="Scripting.Node.Terminal.ObjectRelative(BlockRegionHandle, int3, byte3)"/> to reference blocks inside the <see cref="Scripting.Node.BlockRegion"/>.
        /// </remarks>
        /// <param name="size">Size of the region to create.</param>
        /// <returns>The created <see cref="Scripting.Node.BlockRegion"/>.</returns>
        public Scripting.Node.BlockRegion CreateRegion(int3 size)
        {
            var array = new Array3D<ushort>(size);
            var region = new Scripting.Node.BlockRegion(_graphId, (ushort)_regions.Count, array);
            _regions.Add(region);

            return region;
        }

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
            CollectionsMarshal.AsSpan(_nodes)[handle._index]._settings.Add(setting);
        }

        /// <summary>
        /// Connects a <see cref="Scripting.Node.Terminal"/> to a <see cref="Scripting.Node.Terminal"/>.
        /// </summary>
        /// <remarks>
        /// Ignores connections if either terminal is null (<see cref="Scripting.Node.Terminal.IsNull"/>).
        /// </remarks>
        /// <param name="from">The source <see cref="Scripting.Node.Terminal"/>.</param>
        /// <param name="to">The target <see cref="Scripting.Node.Terminal"/>.</param>
        public void Connect(Scripting.Node.Terminal from, Scripting.Node.Terminal to)
        {
            if (from.IsNull || to.IsNull)
            {
                return;
            }

            _connections.Add(new Scripting.Node.Connection(from, to));
        }

        /// <summary>
        /// Gets the specified node.
        /// </summary>
        /// <param name="handle">Handle of the node.</param>
        /// <returns>The specified node.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref Node GetNode(NodeHandle handle)
        {
            if (handle._graphId != _graphId)
            {
                ThrowArgumentException($"{nameof(handle)} belongs to another {nameof(CodeGraph)}.", nameof(handle));
            }

            return ref GetNode(handle._index);
        }

        /// <summary>
        /// Gets the node at the specified index.
        /// </summary>
        /// <param name="index">Index of the node to get.</param>
        /// <returns>Node at the specified index.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref Node GetNode(int index)
            => ref NodesSpan[index];

        /// <summary>
        /// Enters a new expression scope.
        /// </summary>
        /// <returns>A disposable that exits the scope when disposed.</returns>
        public ExpressionScopeDisposable ExpressionScope()
            => new ExpressionScopeDisposable(this);

        /// <summary>
        /// Enters a new expression scope.
        /// </summary>
        public void EnterExpressionScope()
            => _expressionDepth++;

        /// <summary>
        /// Exits the current expression scope.
        /// </summary>
        public void ExitExpressionScope()
        {
            if (_expressionDepth <= 0)
            {
                ThrowInvalidOperationException("Cannot exit an expression scope, when not inside one.");
            }

            _expressionDepth--;
        }

        /// <summary>
        /// Writes the contents of the builder to another builder and clears this builder.
        /// </summary>
        /// <param name="destination">The destination builder.</param>
        public void WriteToAndClear(FlatBuilder destination)
        {
            UpdateMerged();

            int destinationNodeCount = destination._nodes.Count;
            int destinationRegionCount = destination._regions.Count;
            int destinationConnectionsCount = destination._connections.Count;

            foreach (ref var node in NodesSpan)
            {
                node._handle = new NodeHandle(destination._graphId, (ushort)(destinationNodeCount + node._handle._index));
            }

            foreach (ref var region in RegionsSpan)
            {
                region = new Scripting.Node.BlockRegion(destination._graphId, (ushort)(destinationRegionCount + region._index), region._blocks);
            }

            CollectionsMarshal.SetCount(destination._nodes, destination._nodes.Count + _nodes.Count);
            CollectionsMarshal.SetCount(destination._regions, destination._regions.Count + _regions.Count);
            CollectionsMarshal.SetCount(destination._connections, destination._connections.Count + _connections.Count);

            NodesSpan.CopyTo(destination.NodesSpan[destinationNodeCount..]);
            RegionsSpan.CopyTo(destination.RegionsSpan[destinationRegionCount..]);

            // connection node and region ids get updated on build
            ConnectionsSpan.CopyTo(destination.ConnectionsSpan[destinationConnectionsCount..]);

#pragma warning disable IDE0028 // Simplify collection initialization
            destination._mergedBuilderOffsets ??= new(4);
#pragma warning restore IDE0028 // Simplify collection initialization
            destination._mergedBuilderOffsets.Add(_graphId, (destinationNodeCount, destinationRegionCount));

            Clear();
        }

        /// <summary>
        /// Builds the final <see cref="CodeGraph"/> and clears the builder for reuse.
        /// </summary>
        /// <param name="nestedExpressionsEmittedFirst">
        /// Indicates the order in which nodes were previously emitted by the caller.
        /// <para/>
        /// When <c>true</c>, nodes were placed in an order from more deeply nested to less nested.
        /// <para/>
        /// When <c>false</c>, nodes were placed in an order from less nested to more deeply nested.
        /// </param>
        /// <returns>The constructed <see cref="CodeGraph"/>.</returns>
        public CodeGraph BuildAndClear(bool nestedExpressionsEmittedFirst)
        {
            UpdateMerged();

            var scopeStack = _cachedScopeStack ??= new Stack<CodeScope>(8);
            var rootScope = new CodeScope(ScopeType.Statement);
            scopeStack.Push(rootScope);

            var nodes = new List<NodeData>(_nodes.Count);
            CollectionsMarshal.SetCount(nodes, _nodes.Count);

            int declaringNodeOffset = nestedExpressionsEmittedFirst ? 1 : 0;

            foreach (var node in NodesSpan)
            {
                var targetCount = node._expressionDepth + 1;

                while (targetCount < scopeStack.Count)
                {
                    scopeStack.Pop();
                }

                while (targetCount > scopeStack.Count)
                {
                    var parent = scopeStack.Peek();
                    var childScope = new CodeScope(ScopeType.Expression, parent, declaringNodeOffset: declaringNodeOffset);
                    parent._children.Add(childScope);
                    scopeStack.Push(childScope);
                }

                nodes[node._handle._index] = new NodeData(node.Type, node._settings);
                scopeStack.Peek()._nodes.Add(node._handle);
            }

            scopeStack.Clear();

            var graphId = _graphId;
            var regions = _regions;
            var connections = _connections;

            Clear();

            _ = Builder.RemoveEmptyScopesAndEnsureDeclaringIndexInBounds(rootScope);

            return new CodeGraph(graphId, nodes, rootScope, regions, connections);
        }

        /// <summary>
        /// Clears the contents of the builder.
        /// </summary>
        [MemberNotNull(nameof(_regions), nameof(_connections))]
        public void Clear()
            => Clear(0);

        [MemberNotNull(nameof(_regions), nameof(_connections))]
        private void Clear(int initialCapacity)
        {
#pragma warning disable IDE0028 // Simplify collection initialization
            _nodes.Clear();
            _regions = new(0);
            _connections = new(initialCapacity);
#pragma warning restore IDE0028 // Simplify collection initialization 
            _mergedBuilderOffsets?.Clear();
            _graphId = GetNextGraphId();
            _expressionDepth = 0;
        }

        private ref Node AddNode()
        {
            CollectionsMarshal.SetCount(_nodes, _nodes.Count + 1);
            return ref NodesSpan[^1];
        }

        private void UpdateMerged()
        {
            if (_mergedBuilderOffsets is not { Count: > 0 })
            {
                return;
            }

            foreach (ref var connection in ConnectionsSpan)
            {
                connection = new(UpdateMergedTerminal(connection.From), UpdateMergedTerminal(connection.To));
            }
        }

        private Scripting.Node.Terminal UpdateMergedTerminal(Scripting.Node.Terminal terminal)
        {
            Debug.Assert(_mergedBuilderOffsets is not null);
            var type = terminal.Type;

            if (type is Scripting.Node.TerminalType.Node)
            {
                var node = terminal.Node!.Value;
                if (node._graphId == _graphId || !_mergedBuilderOffsets.TryGetValue(node._graphId, out var mergedIndex))
                {
                    return terminal;
                }

                return terminal.WithGraphId(_graphId, mergedIndex.NodeIndex);
            }
            else if (type is Scripting.Node.TerminalType.ObjectRelative)
            {
                var region = terminal.Region!.Value;
                if (region._graphId == _graphId || !_mergedBuilderOffsets.TryGetValue(region._graphId, out var mergedIndex))
                {
                    return terminal;
                }

                return terminal.WithGraphId(_graphId, mergedIndex.RegionIndex);
            }
            else
            {
                return terminal;
            }
        }

        /// <summary>
        /// Node of a <see cref="FlatBuilder"/>.
        /// </summary>
        [StructLayout(LayoutKind.Auto)]
        public struct Node
        {
            /// <summary>
            /// Type of the node; or <see langword="null"/>, if the node is empty/null.
            /// </summary>
            public BlockDef? Type;
            internal readonly int _expressionDepth;
            internal NodeHandle _handle;
            internal SettingsCollection _settings;

            internal Node(NodeHandle handle, BlockDef? type, int expressionDepth)
            {
                _handle = handle;
                Type = type;
                _expressionDepth = expressionDepth;
            }

            /// <summary>
            /// Gets the handle of the node.
            /// </summary>
            /// <value>Handle of the node.</value>
            public readonly NodeHandle Handle => _handle;

            /// <summary>
            /// Gets the settings of the node.
            /// </summary>
            /// <value>Settings of the node.</value>
            public readonly NodeSettingsCollection Settings => new NodeSettingsCollection(_settings);

            /// <summary>
            /// Gets the expression nesting depth of the node.
            /// </summary>
            /// <remarks>
            /// A value of <c>0</c> indicates that the node belongs to the root <see cref="ScopeType.Statement"/> scope.
            /// <para/>
            /// A value greater than <c>0</c> indicates that the node belongs to a nested <see cref="ScopeType.Expression"/> scope, where the value represents the level of expression nesting.
            /// </remarks>
            /// <value>The zero-based expression nesting depth.</value>
            public readonly int ExpressionDepth => _expressionDepth;

            /// <summary>
            /// Add a setting to the node.
            /// </summary>
            /// <param name="setting">The setting to add.</param>
            public void AddSetting(PrefabSetting setting)
                => _settings.Add(setting);
        }

        /// <summary>
        /// Disposable helper to automatically exit an expression scope.
        /// </summary>
        public struct ExpressionScopeDisposable : IDisposable
        {
            private FlatBuilder? _builder;

            internal ExpressionScopeDisposable(FlatBuilder builder)
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
