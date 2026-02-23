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
using SettingsCollection = BitcoderCZ.Buffers.InlineList<BitcoderCZ.Buffers.FixedArray1<BitcoderCZ.Fancade.PrefabSetting>, BitcoderCZ.Fancade.PrefabSetting>;

namespace BitcoderCZ.Fancade.Editing.Scripting;

/// <summary>
/// Represents a structured graph of code nodes and connections.
/// </summary>
public sealed partial class CodeGraph
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
    /// Gets the id of the graph.
    /// </summary>
    /// <value>Id of the graph.</value>
    public int Id => _id;

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
        DeclaringNodeIndex = Math.Max(parent._nodes.Count - 1, 0);
    }

    internal CodeScope(ScopeType type, CodeScope parent, int declaringNodeOffset)
    {
        Type = type;
        Parent = parent;
        DeclaringNodeIndex = Math.Max(parent._nodes.Count - 1 + declaringNodeOffset, 0);
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
    public int? DeclaringNodeIndex { get; internal set; }

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
