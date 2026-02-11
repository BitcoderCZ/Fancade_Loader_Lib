// <copyright file="CodeGraphEmitHelper.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System.Buffers;
using BitcoderCZ.Fancade.Editing.Utils;
using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade.Editing.Scripting.Utils;

internal static class CodeGraphEmitHelper
{
    private static readonly int3 FromToOutsidePosition = int3.One * Connection.IsFromToOutsideValue;
    private static readonly int3 Padding = int3.One;

    public static int3[] Pack(int3 graphSize, ReadOnlySpan<Node.BlockRegion> regions)
    {
        int sizesCount = 1 + regions.Length;
        var sizes = ArrayPool<int3>.Shared.Rent(sizesCount);

        sizes[0] = graphSize + Padding;

        int index = 0;
        foreach (var region in regions)
        {
            sizes[index++] = region.Blocks.Size + Padding;
        }

        var positions = BinPacker.Compute(sizes, sizesCount);

        ArrayPool<int3>.Shared.Return(sizes);

        return positions;
    }

    public static int3 GetAbsolutePosition(PositionedNode.Terminal terminal, Func<int, int3> getNodePosition, Func<PositionedNode.BlockRegion, int3> getRegionPosition)
        => terminal.Type switch
        {
            PositionedNode.TerminalType.Node => getNodePosition(terminal.NodeIndex!.Value),
            PositionedNode.TerminalType.OutsideInput or PositionedNode.TerminalType.OutsideOutput => FromToOutsidePosition,
            PositionedNode.TerminalType.ObjectAbsolute => terminal.BlockPostion!.Value,
            PositionedNode.TerminalType.ObjectRelative => getRegionPosition(terminal.Region!) + terminal.BlockPostion!.Value,
            _ => default,
        };

    public static Connection NodeConnectionToConnection(PositionedNode.Connection connection, Func<int, int3> getNodePosition, Func<PositionedNode.BlockRegion, int3> getRegionPosition)
        => new Connection(GetAbsolutePosition(connection.From, getNodePosition, getRegionPosition), GetAbsolutePosition(connection.To, getNodePosition, getRegionPosition), connection.From.VoxelPosition, connection.To.VoxelPosition);
}