// <copyright file="BlockObstructionInfo.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade;

internal readonly struct BlockObstructionInfo
{
    public readonly int PrefabId;
    public readonly int3 PrefabPosition;
    public readonly int3 ObstructedPosition;

    public BlockObstructionInfo(int prefabId, int3 position, int3 obstructedPosition)
    {
        PrefabId = prefabId;
        PrefabPosition = position;
        ObstructedPosition = obstructedPosition;
    }
}
