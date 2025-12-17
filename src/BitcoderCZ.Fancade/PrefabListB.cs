// <copyright file="PrefabListB.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Fancade.Utils;
using BitcoderCZ.Maths.Vectors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace BitcoderCZ.Fancade;

// TODO: Replace PrefabList
#pragma warning disable CS1591
#pragma warning disable SA1600
#pragma warning disable SA1203 // Constants should appear before fields
public sealed class PrefabListB
{
    private static readonly int StockCount = BitcoderCZ.Fancade.Raw.RawGame.CurrentNumbStockPrefabs;
    private const int MaxCustomCount = 512;

    private readonly Prefab?[] _prefabs;
    private readonly SegmentData[] _segments;

    // make setable
    private int _permanentIdCounter = StockCount;

    private int _customPrefabCount;
    private int _customSegmentCount;

    public PrefabListB()
        : this(MaxCustomCount)
    {
    }

    public PrefabListB(int maxCustomCount)
    {
        ThrowHelper.ThrowIfNegative(maxCustomCount);

        _prefabs = new Prefab?[StockCount + maxCustomCount];
        _segments = new SegmentData[StockCount + maxCustomCount];
    }

    private PrefabListB(Prefab?[] prefabs, SegmentData[] segments)
    {
        _prefabs = prefabs;
        _segments = segments;
    }

    public int CustomPrefabCount => _customPrefabCount;

    public int TotalSegmentCount => StockCount + _customSegmentCount;

    public int CustomSegmentCount => _customSegmentCount;

    public PrefabEnumerable AllPrefabs => new PrefabEnumerable(_prefabs, 0, StockCount + _customSegmentCount);

    public PrefabEnumerable StockPrefabs => new PrefabEnumerable(_prefabs, 0, StockCount);

    public PrefabEnumerable CustomPrefabs => new PrefabEnumerable(_prefabs, StockCount, _customSegmentCount);

    // TODO: extension method in editing
    public static PrefabListB CreateStock()
    {
        var list = new PrefabListB();
        list.LoadStock();
        return list;
    }

    // TODO: extension method in editing
    public void LoadStock()
    {
        var terminalBuilder = ImmutableArray.CreateBuilder<TerminalInfo>();
        foreach (var prefab in StockBlocks.PrefabList)
        {
            PrefabTerminalInfo? terminalInfo = null;

            if (StockBlocks.TryGetBlockDef(prefab.Id, out var def) && def.Terminals.Length > 0)
            {
                foreach (var terminal in def.Terminals)
                {
                    TerminalDirection direction;
                    if (terminal.Position.X == 0)
                    {
                        direction = TerminalDirection.NegativeX;
                    }
                    else if (terminal.Position.Z == 0)
                    {
                        direction = TerminalDirection.NegativeZ;
                    }
                    else if (terminal.Position.X % 8 == 6)
                    {
                        direction = TerminalDirection.PositiveX;
                    }
                    else
                    {
                        UnityEngine.Debug.Assert(terminal.Position.Z % 8 == 6);
                        direction = TerminalDirection.PositiveZ;
                    }

                    terminalBuilder.Add(new TerminalInfo(terminal.Position, terminal.SignalType, direction, terminal.Type is TerminalType.In)
                    {
                        Name = terminal.Name,
                    });
                }

                terminalInfo = new PrefabTerminalInfo(terminalBuilder.DrainToImmutable());
            }

            SetPrefabInternal(prefab, terminalInfo);
        }
    }

    private void SetPrefabInternal(Prefab prefab)
    {
        var prefabSegments = new Dictionary<byte3, int>(prefab.Count);
        foreach (var (segment, segmentId) in prefab.EnumerateWithId())
        {
            prefabSegments[(byte3)segment.PosInPrefab] = segmentId;
            _segments[segmentId] = new SegmentData(prefab.Id, (byte3)segment.PosInPrefab, segment.Voxels);
        }

        //Interlocked.Increment(ref _permanentIdCounter)
        _prefabs[prefab.Id] = prefab;

        /*foreach (var (_, segmentId) in prefab.EnumerateWithId())
        {
            _segments[segmentId].UpdateMesh(this);
        }*/
    }



    public readonly struct PrefabEnumerable : IEnumerable<KeyValuePair<int, Prefab>>
    {
        private readonly Prefab?[] _prefabs;
        private readonly int _skip;
        private readonly int _take;

        internal PrefabEnumerable(Prefab?[] prefabs, int skip, int take)
        {
            _prefabs = prefabs;
            _skip = skip;
            _take = take;
        }

        public PrefabEnumerator GetEnumerator()
            => new PrefabEnumerator(_prefabs, _skip, _take);

        IEnumerator<KeyValuePair<int, Prefab>> IEnumerable<KeyValuePair<int, Prefab>>.GetEnumerator()
            => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }

    public struct PrefabEnumerator : IEnumerator<KeyValuePair<int, Prefab>>
    {
        private readonly Prefab?[] _prefabs;
        private readonly int _skip;
        private readonly int _take;
        private int _index;
        private KeyValuePair<int, Prefab> _current;

        internal PrefabEnumerator(Prefab?[] prefabs, int skip, int take)
        {
            _prefabs = prefabs;
            _skip = skip;
            _take = take;
            _index = -1;
            _current = default;
        }

        public readonly KeyValuePair<int, Prefab> Current => _current;

        readonly object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            while (++_index < _take)
            {
                int actualIndex = _skip + _index;
                var prefab = _prefabs[actualIndex];
                if (prefab is not null)
                {
                    _current = new KeyValuePair<int, Prefab>(actualIndex, prefab);
                    return true;
                }
            }

            return false;
        }

        public void Reset()
        {
            _index = -1;
            _current = default;
        }

        public readonly void Dispose()
        {
        }
    }

    internal struct SegmentData : IEquatable<SegmentData>
    {
        private readonly Voxels _voxels;

        public SegmentData(int prefabId, byte3 posInPrefab, Voxels voxels)
            : this(prefabId, posInPrefab, voxels, Voxels.Empty)
        {
        }

        public SegmentData(int prefabId, byte3 posInPrefab, Voxels voxels, Voxels visibleFaces)
        {
            PrefabId = prefabId;
            PosInPrefab = posInPrefab;
            _voxels = voxels;
            VisibleFaces = visibleFaces;
        }

        public int PrefabId { get; internal set; }

        public byte3 PosInPrefab { get; internal set; }

        public readonly Voxels Voxels => _voxels;

        public Voxels VisibleFaces { get; private set; }

        public object? UserData { get; set; }

        public static bool operator ==(SegmentData left, SegmentData right)
            => left.PrefabId == right.PrefabId && left.PosInPrefab == right.PosInPrefab;

        public static bool operator !=(SegmentData left, SegmentData right)
            => !(left == right);

        public readonly bool Equals(SegmentData other)
            => this == other;

        public readonly override int GetHashCode()
            => HashCode.Combine(PrefabId, PosInPrefab);

        public readonly override bool Equals(object? obj)
            => obj is SegmentData other && Equals(other);
    }
}
