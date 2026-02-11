// <copyright file="PrefabUsedCache.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Fancade.Raw;
using BitcoderCZ.Maths.Vectors;
using System.Diagnostics;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("BitcoderCZ.Fancade.Runtime.Simulated.Bullet")]

namespace BitcoderCZ.Fancade.Runtime.Simulated.Utils;

internal sealed class PrefabUsedCache
{
    private readonly bool[] _used;

    private PrefabUsedCache(bool[] used)
    {
        _used = used;
    }

    public static PrefabUsedCache Create(PrefabList prefabs, ushort mainId)
    {
        bool[] used = new bool[RawGame.CurrentNumbStockPrefabs + prefabs.SegmentCount];

        used[mainId] = true;
        var createAction = new CreateAction(used, prefabs, StockBlocks.PrefabList);
        prefabs.GetPrefab(mainId).Blocks.EnumerateNonEmptyBlocks(createAction);

        return new PrefabUsedCache(used);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Used(ushort id)
        => _used[id];

    private sealed class CreateAction : IRefValueAction<ushort, int3>
    {
        private readonly bool[] _used;
        private readonly PrefabList _prefabs;
        private readonly PrefabList _stockPrefabs;

        public CreateAction(bool[] used, PrefabList prefabs, PrefabList stockPrefabs)
        {
            _used = used;
            _prefabs = prefabs;
            _stockPrefabs = stockPrefabs;
        }

        public void Invoke(ref ushort id, int3 segmentPos)
        {
            if (_used[id])
            {
                return;
            }

            _used[id] = true;

            var segment = id < RawGame.CurrentNumbStockPrefabs ? _stockPrefabs.GetSegment(id) : _prefabs.GetSegment(id);

            if (segment.PrefabId != id)
            {
                return;
            }

            var prefab = segment.PrefabId < RawGame.CurrentNumbStockPrefabs ? _stockPrefabs.GetPrefab(segment.PrefabId) : _prefabs.GetPrefab(segment.PrefabId);

            prefab.Blocks.EnumerateNonEmptyBlocks(this);
        }
    }
}
