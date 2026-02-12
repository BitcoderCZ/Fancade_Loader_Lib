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
using SettingsCollection = BitcoderCZ.Buffers.InlineList<BitcoderCZ.Buffers.FixedArray2<BitcoderCZ.Fancade.PrefabSetting>, BitcoderCZ.Fancade.PrefabSetting>;

namespace BitcoderCZ.Fancade.Editing.Scripting;

/// <summary>
/// Represents a positioned graph of code nodes and connections.
/// </summary>
public sealed class PositionedCodeGraph
{
    private readonly List<PositionedNode> _nodes;
    private readonly List<PositionedNode.BlockRegion> _regions;
    private readonly List<PositionedNode.Connection> _connections;

    private PositionedCodeGraph(List<PositionedNode> nodes, List<PositionedNode.BlockRegion> regions, List<PositionedNode.Connection> connections, int3 size, int3 offset)
    {
        _nodes = nodes;
        _regions = regions;
        _connections = connections;
        Size = size;
        Offset = offset;
    }

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
    /// Gets the positioned nodes contained in the graph.
    /// </summary>
    /// <value>A read-only list of positioned nodes.</value>
    public IReadOnlyList<PositionedNode> Nodes => _nodes;

    /// <summary>
    /// Gets the positioned nodes contained in the graph as a span.
    /// </summary>
    /// <value>A read-only span of positioned nodes.</value>
    public ReadOnlySpan<PositionedNode> NodesSpan => CollectionsMarshal.AsSpan(_nodes);

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
    public IReadOnlyList<PositionedNode.Connection> Connections => _connections;

    /// <summary>
    /// Gets the connections between nodes in the graph as a span.
    /// </summary>
    /// <value>A read-only span of node connections.</value>
    public ReadOnlySpan<PositionedNode.Connection> ConnectionSpan => CollectionsMarshal.AsSpan(_connections);

    internal static PositionedCodeGraph Create(List<PositionedNode> nodes, ReadOnlySpan<Node.BlockRegion> regions, ReadOnlySpan<Node.Connection> connections, int3 size)
    {
        var positions = CodeGraphEmitHelper.Pack(size, regions);

        var offset = positions[0];

        var positionedRegions = new List<PositionedNode.BlockRegion>(regions.Length);
        CollectionsMarshal.SetCount(positionedRegions, regions.Length);
        var positionedRegionsSpan = CollectionsMarshal.AsSpan(positionedRegions);

        foreach (var region in regions)
        {
            positionedRegionsSpan[region._index] = new PositionedNode.BlockRegion(region.Blocks, positions[region._index + 1], region._index);
        }

        var positionedConnections = new List<PositionedNode.Connection>(connections.Length);
        CollectionsMarshal.SetCount(positionedConnections, connections.Length);
        var positionedConnectionsSpan = CollectionsMarshal.AsSpan(positionedConnections);

        int index = 0;
        foreach (var connection in connections)
        {
            positionedConnectionsSpan[index++] = PositionedNode.Connection.Cast(connection, region => positionedRegions[region._index]);
        }

        return new PositionedCodeGraph(nodes, positionedRegions, positionedConnections, size, offset);
    }
}