// <copyright file="BlockObstructionInfo.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade;

internal readonly struct BlockObstructionInfo
{
    public readonly string PrefabName;
    public readonly int3 PrefabPosition;
    public readonly int3 ObstructedPosition;

    public BlockObstructionInfo(string prefabName, int3 position, int3 obstructedPosition)
    {
        PrefabName = prefabName;
        PrefabPosition = position;
        ObstructedPosition = obstructedPosition;
    }
}
