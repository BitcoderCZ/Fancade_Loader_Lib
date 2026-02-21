// <copyright file="PositionedCodeGraph.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BitcoderCZ.Fancade.Editing.Scripting.Utils;
using BitcoderCZ.Maths.Vectors;
using static BitcoderCZ.Utils.ThrowHelper;
using SettingsCollection = BitcoderCZ.Buffers.InlineList<BitcoderCZ.Buffers.FixedArray1<BitcoderCZ.Fancade.PrefabSetting>, BitcoderCZ.Fancade.PrefabSetting>;

namespace BitcoderCZ.Fancade.Editing.Scripting;

/// <summary>
/// Represents a positioned graph of code nodes and connections.
/// </summary>
public sealed class PositionedCodeGraph
{
    /// <summary>
    /// Gets an empty <see cref="PositionedCodeGraph"/> instance.
    /// </summary>
    /// <value>An empty <see cref="PositionedCodeGraph"/> instance.</value>
    public static PositionedCodeGraph Empty { get; } = new PositionedCodeGraph(FirstId, [], [], [], int3.Zero, int3.Zero);

    internal readonly ushort _id;
    internal readonly List<PositionedNodeData> _nodes;
    internal readonly List<PositionedNode.BlockRegion> _regions;
    internal readonly List<Node.Connection> _connections;

    private const int FirstId = ushort.MaxValue / 2;

    private static int nextGraphId = FirstId;

    private PositionedCodeGraph(ushort id, List<PositionedNodeData> nodes, List<PositionedNode.BlockRegion> regions, List<Node.Connection> connections, int3 size, int3 offset)
    {
        _id = id;
        _nodes = nodes;
        _regions = regions;
        _connections = connections;
        Size = size;
        Offset = offset;
    }

    /// <summary>
    /// Gets the id of the graph.
    /// </summary>
    /// <value>Id of the graph.</value>
    public int Id => _id;

    /// <summary>
    /// Gets the size of the graph.
    /// </summary>
    /// <value>Size of the region containing all nodes, does not include <see cref="Offset"/> or <see cref="Regions"/>.</value>
    public int3 Size { get; }

    /// <summary>
    /// Gets the offset of the nodes in the graph.
    /// </summary>
    /// <value>Offset of the nodes in the graph.</value>
    public int3 Offset { get; }

    /// <summary>
    /// Gets the <see cref="Node.BlockRegion"/>s in the <see cref="PositionedCodeGraph"/>.
    /// </summary>
    /// <value>The <see cref="Node.BlockRegion"/>s in the <see cref="PositionedCodeGraph"/>.</value>
    public IReadOnlyList<PositionedNode.BlockRegion> Regions => _regions;

    /// <summary>
    /// Gets the <see cref="Node.BlockRegion"/>s in the <see cref="PositionedCodeGraph"/>.
    /// </summary>
    /// <value>The <see cref="Node.BlockRegion"/>s in the <see cref="PositionedCodeGraph"/>.</value>
    public ReadOnlySpan<PositionedNode.BlockRegion> RegionsSpan => CollectionsMarshal.AsSpan(_regions);

    /// <summary>
    /// Gets the connections between nodes in the graph.
    /// </summary>
    /// <value>A read-only list of node connections.</value>
    public IReadOnlyList<Node.Connection> Connections => _connections;

    /// <summary>
    /// Gets the connections between nodes in the graph as a span.
    /// </summary>
    /// <value>A read-only span of node connections.</value>
    public ReadOnlySpan<Node.Connection> ConnectionSpan => CollectionsMarshal.AsSpan(_connections);

    internal ReadOnlySpan<PositionedNodeData> NodesSpan => CollectionsMarshal.AsSpan(_nodes);

    /// <summary>
    /// Gets a node from the graph.
    /// </summary>
    /// <param name="index">Index of the node to get.</param>
    /// <param name="settings">Settings of the node.</param>
    /// <returns>The node.</returns>
    public PositionedNode GetNode(int index, out NodeSettingsCollection settings)
    {
        var data = _nodes[index];
        if (data.Type is null)
        {
            settings = default;
            return PositionedNode.Empty;
        }

        settings = new NodeSettingsCollection(data._settings);
        return new PositionedNode(_id, (ushort)index, data.Type, data._offset);
    }

    /// <summary>
    /// Gets a node from the graph.
    /// </summary>
    /// <param name="handle">Handle of the node to get.</param>
    /// <param name="settings">Settings of the node.</param>
    /// <returns>The node.</returns>
    public PositionedNode GetNode(NodeHandle handle, out NodeSettingsCollection settings)
    {
        if (handle._graphId != _id)
        {
            ThrowArgumentException($"{nameof(handle)} belongs to another {nameof(CodeGraph)}.", nameof(handle));
        }

        var data = _nodes[handle._index];
        if (data.Type is null)
        {
            settings = default;
            return PositionedNode.Empty;
        }

        settings = new NodeSettingsCollection(data._settings);
        return new PositionedNode(_id, handle._index, data.Type, data._offset);
    }

    internal static PositionedCodeGraph Create(List<PositionedNodeData> nodes, ReadOnlySpan<Node.BlockRegion> regions, ReadOnlySpan<Node.Connection> connections, int3 size)
    {
        var graphId = GetNextGraphId();

        var positions = CodeGraphEmitHelper.Pack(size, regions);

        var offset = positions[0];

        var positionedRegions = new List<PositionedNode.BlockRegion>(regions.Length);
        CollectionsMarshal.SetCount(positionedRegions, regions.Length);
        var positionedRegionsSpan = CollectionsMarshal.AsSpan(positionedRegions);

        foreach (var region in regions)
        {
            positionedRegionsSpan[region._index] = new PositionedNode.BlockRegion(graphId, region._index, region.Blocks, positions[region._index + 1]);
        }

        var positionedConnections = new List<Node.Connection>(connections.Length);
        CollectionsMarshal.SetCount(positionedConnections, connections.Length);
        var positionedConnectionsSpan = CollectionsMarshal.AsSpan(positionedConnections);

        int index = 0;
        foreach (var connection in connections)
        {
            positionedConnectionsSpan[index++] = connection.WithGraphIdInternal(graphId);
        }

        return new PositionedCodeGraph(graphId, nodes, positionedRegions, positionedConnections, size, offset);
    }

    private static ushort GetNextGraphId()
        => (ushort)Interlocked.Increment(ref nextGraphId);
}