// <copyright file="CodeGraphPositionHelpers.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System.Runtime.InteropServices;
using BitcoderCZ.Maths.Vectors;
using BitcoderCZ.Utils;

namespace BitcoderCZ.Fancade.Editing.Scripting.Utils;

internal static class CodeGraphPositionHelpers
{
    public static PositionedCodeGraph LayoutGraphs<TArg0>(ReadOnlySpan<CodeGraph> graphs, LayoutNodes<TArg0> layoutNodes, TArg0 arg0)
    {
        if (graphs.IsEmpty)
        {
            return PositionedCodeGraph.Empty;
        }

        var graphToOffset = new Dictionary<ushort, (int NodeOffset, int RegionOffset)>(graphs.Length);

        int totalNodeCount = 0;
        int totalRegionCount = 0;
        int totalConnectionCount = 0;
        foreach (var graph in graphs)
        {
            graphToOffset.Add(graph._id, (totalNodeCount, totalRegionCount));
            totalNodeCount += graph.NodeCount;
            totalRegionCount += graph._regions.Count;
            totalConnectionCount += graph._connections.Count;
        }

        var nodes = new List<PositionedNodeData>(totalNodeCount);
        CollectionsMarshal.SetCount(nodes, totalNodeCount);
        var nodesSpan = CollectionsMarshal.AsSpan(nodes);

        var regions = new List<Node.BlockRegion>(totalRegionCount);
        var connections = new List<Node.Connection>(totalConnectionCount);

        int3 totalSize = int3.Zero;
        int3 graphOffset = int3.Zero;

        int nodeOffset = 0;
        int regionOffset = 0;
        foreach (var graph in graphs)
        {
            var size = graph.NodeCount is 0 ? int3.Zero : layoutNodes(graph, nodesSpan.Slice(nodeOffset, graph.NodeCount), graphOffset, arg0);

            foreach (var region in graph.RegionsSpan)
            {
                regions.Add(new Node.BlockRegion(graph._id, (ushort)(nodeOffset + region._index), region._blocks));
            }

            foreach (var connection in graph.ConnectionsSpan)
            {
                connections.Add(new Node.Connection(UpdateTerminalIndex(connection.From), UpdateTerminalIndex(connection.To)));
            }

            totalSize = int3.Max(totalSize, graphOffset + size);
            graphOffset.X += size.X + 1;

            nodeOffset += graph.NodeCount;
            regionOffset += graph._regions.Count;

            Node.Terminal UpdateTerminalIndex(Node.Terminal terminal)
            {
                switch (terminal.Type)
                {
                    case Node.TerminalType.Node:
                        {
                            var nodeHandle = terminal.Node!.Value;

                            int indexOffset = default;
                            if (nodeHandle._graphId == graph._id)
                            {
                                indexOffset = nodeOffset;
                            }
                            else
                            {
                                if (graphToOffset.TryGetValue(nodeHandle._graphId, out var item))
                                {
                                    indexOffset = item.NodeOffset;
                                }
                                else
                                {
                                    ThrowHelper.ThrowArgumentException($"Connection to a graph not in {nameof(graphs)}.", nameof(graphs));
                                }
                            }

                            return terminal.WithGraphIdInternal(nodeHandle._graphId, indexOffset);
                        }

                    case Node.TerminalType.ObjectRelative:
                        {
                            var regionHandle = terminal.Region!.Value;

                            int indexOffset = default;
                            if (regionHandle._graphId == graph._id)
                            {
                                indexOffset = regionOffset;
                            }
                            else
                            {
                                if (graphToOffset.TryGetValue(regionHandle._graphId, out var item))
                                {
                                    indexOffset = item.RegionOffset;
                                }
                                else
                                {
                                    ThrowHelper.ThrowArgumentException($"Connection to a graph not in {nameof(graphs)}.", nameof(graphs));
                                }
                            }

                            return terminal.WithGraphIdInternal(regionHandle._graphId, indexOffset);
                        }

                    default:
                        return terminal;
                }
            }
        }

        return PositionedCodeGraph.Create(nodes, CollectionsMarshal.AsSpan(regions), CollectionsMarshal.AsSpan(connections), totalSize);
    }

    public static PositionedCodeGraph LayoutGraphs<TArg0>(IEnumerable<CodeGraph> graphs, LayoutNodes<TArg0> layoutNodes, TArg0 arg0)
    {
        int? graphCount = null;
        {
            if (graphs.TryGetNonEnumeratedCount(out var count))
            {
                graphCount = count;
            }
        }

        if (graphCount is 0)
        {
            return PositionedCodeGraph.Empty;
        }

        var graphToOffset = new Dictionary<ushort, (int NodeOffset, int RegionOffset)>(graphCount ?? 4);

        int totalNodeCount = 0;
        int totalRegionCount = 0;
        int totalConnectionCount = 0;
        foreach (var graph in graphs)
        {
            graphToOffset.Add(graph._id, (totalNodeCount, totalRegionCount));
            totalNodeCount += graph.NodeCount;
            totalRegionCount += graph._regions.Count;
            totalConnectionCount += graph._connections.Count;
        }

        var nodes = new List<PositionedNodeData>(totalNodeCount);
        CollectionsMarshal.SetCount(nodes, totalNodeCount);
        var nodesSpan = CollectionsMarshal.AsSpan(nodes);

        var regions = new List<Node.BlockRegion>(totalRegionCount);
        var connections = new List<Node.Connection>(totalConnectionCount);

        int3 totalSize = int3.Zero;
        int3 graphOffset = int3.Zero;

        int nodeOffset = 0;
        int regionOffset = 0;
        foreach (var graph in graphs)
        {
            var size = layoutNodes(graph, nodesSpan.Slice(nodeOffset, graph.NodeCount), graphOffset, arg0);

            foreach (var region in graph.RegionsSpan)
            {
                regions.Add(new Node.BlockRegion(graph._id, (ushort)(nodeOffset + region._index), region._blocks));
            }

            foreach (var connection in graph.ConnectionsSpan)
            {
                connections.Add(new Node.Connection(UpdateTerminalIndex(connection.From), UpdateTerminalIndex(connection.To)));
            }

            totalSize = int3.Max(totalSize, graphOffset + size);
            graphOffset.X += size.X + 1;

            nodeOffset += graph.NodeCount;
            regionOffset += graph._regions.Count;

            Node.Terminal UpdateTerminalIndex(Node.Terminal terminal)
            {
                switch (terminal.Type)
                {
                    case Node.TerminalType.Node:
                        {
                            var nodeHandle = terminal.Node!.Value;

                            int indexOffset = default;
                            if (nodeHandle._graphId == graph._id)
                            {
                                indexOffset = nodeOffset;
                            }
                            else
                            {
                                if (graphToOffset.TryGetValue(nodeHandle._graphId, out var item))
                                {
                                    indexOffset = item.NodeOffset;
                                }
                                else
                                {
                                    ThrowHelper.ThrowArgumentException($"Connection to a graph not in {nameof(graphs)}.", nameof(graphs));
                                }
                            }

                            return terminal.WithGraphIdInternal(nodeHandle._graphId, indexOffset);
                        }

                    case Node.TerminalType.ObjectRelative:
                        {
                            var regionHandle = terminal.Region!.Value;

                            int indexOffset = default;
                            if (regionHandle._graphId == graph._id)
                            {
                                indexOffset = regionOffset;
                            }
                            else
                            {
                                if (graphToOffset.TryGetValue(regionHandle._graphId, out var item))
                                {
                                    indexOffset = item.RegionOffset;
                                }
                                else
                                {
                                    ThrowHelper.ThrowArgumentException($"Connection to a graph not in {nameof(graphs)}.", nameof(graphs));
                                }
                            }

                            return terminal.WithGraphIdInternal(regionHandle._graphId, indexOffset);
                        }

                    default:
                        return terminal;
                }
            }
        }

        return PositionedCodeGraph.Create(nodes, CollectionsMarshal.AsSpan(regions), CollectionsMarshal.AsSpan(connections), totalSize);
    }
}

internal delegate int3 LayoutNodes<TArg0>(CodeGraph graph, Span<PositionedNodeData> nodes, int3 offset, TArg0 arg0);