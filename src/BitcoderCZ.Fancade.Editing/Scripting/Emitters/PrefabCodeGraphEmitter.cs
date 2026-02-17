// <copyright file="PrefabCodeGraphEmitter.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System.Diagnostics;
using System.Runtime.InteropServices;
using BitcoderCZ.Fancade.Editing.Scripting.Utils;
using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade.Editing.Scripting.Emitters;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable SA1600 // Elements should be documented
public static class PrefabCodeGraphEmitter
{
    public static int3 Emit(PositionedCodeGraph graph, Prefab prefab, int3 position)
    {
        var blocks = prefab.Blocks;
        var size = graph.Offset + graph.Size;

        var blocksOffset = position + graph.Offset;
        if (!graph.NodesSpan.IsEmpty)
        {
            blocks.ReserveRegion(blocksOffset, blocksOffset + graph.Size - int3.One);
            foreach (var node in graph.NodesSpan)
            {
                if (node._type is null)
                {
                    continue;
                }

                var blockPos = blocksOffset + node.Offset;
                blocks.SetPrefab(blockPos, node.Type.Prefab);

                if (node._settings.Count > 0)
                {
                    ref var settings = ref CollectionsMarshal.GetValueRefOrAddDefault(prefab.Settings, blockPos, out _);

                    foreach (var setting in node._settings)
                    {
                        settings = settings.Add(setting);
                    }
                }
            }
        }

        foreach (var region in graph.RegionsSpan)
        {
            var packedPos = region.Offset;
            blocks.WriteRegion(position + packedPos, region.Blocks);

            size = int3.Max(size, packedPos + region.Blocks.Size);
        }

        foreach (var connection in graph.ConnectionSpan)
        {
            prefab.Connections.Add(CodeGraphEmitHelper.NodeConnectionToConnection(
                connection, 
                nodeHandle =>
                {
                    Debug.Assert(nodeHandle._graphId == graph._id);
                    return blocksOffset + graph._nodes[nodeHandle._index].Offset;
                },
                regionHandle =>
                {
                    Debug.Assert(regionHandle._graphId == graph._id);
                    return position + graph._regions[regionHandle._index].Offset;
                }));
        }

        return size;
    }
}