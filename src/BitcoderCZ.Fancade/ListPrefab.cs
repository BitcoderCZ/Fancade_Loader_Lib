// <copyright file="PrefabB.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Fancade.Utils;
using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade;

#pragma warning disable CS1591
#pragma warning disable SA1600
public sealed class ListPrefab
{
    public const int MaxSize = BitcoderCZ.Fancade.Prefab.MaxSize;

    // id that doesn't ever change, for example when the normal id is shifted due to added/removed segments
    public readonly int PermanentId;
    public int Id;
    public string Name;
    public PrefabType Type;
    public PrefabCollider Collider;
    public FcColor BackgroundColor;
    public PrefabTerminalInfo Terminals;

    public Dictionary<byte3, int> Segments;

    internal ListPrefab(int permanentId, string name, PrefabType type, PrefabCollider collider, FcColor backgroundColor, PrefabTerminalInfo terminals)
    {
        PermanentId = permanentId;
        Name = name;
        Type = type;
        Collider = collider;
        BackgroundColor = backgroundColor;
        Terminals = terminals;
        Segments = new(4);
    }

    public List<Connection> Connections { get; }

    public BlockData Blocks { get; }

    // TODO: cache?
    public int3 Size
    {
        get
        {
            var maxPos = byte3.Zero;
            foreach (var pos in Segments.Keys)
            {
                maxPos = byte3.Max(maxPos, pos);
            }

            return maxPos + byte3.One;
        }
    }

    public IEnumerable<KeyValuePair<byte3, int>> PosOrderedValues
    {
        get
        {
            for (int z = 0; z < Size.Z; z++)
            {
                for (int y = 0; y < Size.Y; y++)
                {
                    for (int x = 0; x < Size.X; x++)
                    {
                        var pos = new byte3(x, y, z);
                        if (Segments.TryGetValue(pos, out var segment))
                        {
                            yield return new(pos, segment);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Determines the index of a segment, if it was at the specified position.
    /// </summary>
    /// <param name="key">Position of the segment.</param>
    /// <returns>The index of the segment if it is in bounds; otherwise, <c>-1</c>.</returns>
    public int GetNewSegmentIndex(int3 key)
    {
        int index = 0;

        for (int z = 0; z < MaxSize; z++)
        {
            for (int y = 0; y < MaxSize; y++)
            {
                for (int x = 0; x < MaxSize; x++)
                {
                    var pos = new byte3(x, y, z);

                    if (pos == key)
                    {
                        return index;
                    }

                    if (Segments.ContainsKey(pos))
                    {
                        index++;
                    }
                }
            }
        }

        return -1;
    }

    public bool RemoveSegment(byte3 posInPrefab, out int segmentId, out int3 shift)
    {
        if (Segments.Count <= 1)
        {
            segmentId = 0;
            shift = int3.Zero;
            return false;
        }

        bool removed = Segments.Remove(posInPrefab, out segmentId);

        if (removed)
        {
            shift = ShiftToZero();
        }
        else
        {
            shift = int3.Zero;
        }

        return removed;
    }

    public int CopySegmentsTo(Span<KeyValuePair<byte3, int>> dest)
    {
        if (dest.Length < Segments.Count)
        {
            ThrowHelper.ThrowArgumentOutOfRangeException(nameof(dest), $"{nameof(dest)} must be longer than or equal to {nameof(Segments)}.");
        }

        int i = 0;
        foreach (var item in Segments)
        {
            dest[i++] = item;
        }

        return i;
    }

    private int3 ShiftToZero()
    {
        var minPos = new int3(int.MaxValue, int.MaxValue, int.MaxValue);

        foreach (var pos in Segments.Keys)
        {
            minPos = int3.Min(minPos, pos);
        }

        if (minPos == int3.Zero)
        {
            return int3.Zero;
        }

        for (int z = minPos.Z; z < MaxSize; z++)
        {
            for (int y = minPos.Y; y < MaxSize; y++)
            {
                for (int x = minPos.X; x < MaxSize; x++)
                {
                    var pos = new byte3(x, y, z);

                    if (Segments.TryGetValue(pos, out var segmentId))
                    {
                        Segments.Remove(pos);
                        Segments.Add((byte3)(pos - minPos), segmentId);
                    }
                }
            }
        }

        return minPos;
    }
}