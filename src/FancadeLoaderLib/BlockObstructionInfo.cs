﻿using MathUtils.Vectors;

namespace FancadeLoaderLib;

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
