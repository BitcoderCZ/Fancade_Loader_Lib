// <copyright file="BinPacker.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Maths.Vectors;
using System.Diagnostics;

namespace BitcoderCZ.Fancade.Editing.Utils;

internal static class BinPacker
{
    public static int3[] Compute(int3[] sizes)
    {
        List<Container> placedContainers = [];

        // the result positions
        int3[] positions = new int3[sizes.Length];

        int3 occupiedArea = int3.Zero;
        List<int3> freePositions =
        [
            int3.Zero
        ];

        foreach (var (index, size) in sizes
            .Select((size, index) => (index, size))
            .OrderByDescending(item => item.size.X * item.size.Y * item.size.Z))
        {
            if (freePositions.Count == 0)
            {
                Debug.Fail("No free positions left (this shouldn't happen).");
                return [];
            }
            else if (freePositions.Count == 1)
            {
                if (new Container(freePositions[0], size).IntersectsAny(placedContainers))
                {
                    Debug.Fail("No free (un-occupied) positions left (this shouldn't happen).");
                    return [];
                }

                positions[index] = freePositions[0];
                AddContainer(freePositions[0], size);
                continue;
            }

            // pick the position adding the least to occupiedArea
            var (pos, _) = NetStandardHacks.IEnumerableUtils.MinBy(
                freePositions
                    .Where(pos => !new Container(pos, size).IntersectsAny(placedContainers))
                    .Select(pos => (pos, CalculateArea(int3.Max(pos + size, occupiedArea)))),
                item => item.Item2);

            positions[index] = pos;
            AddContainer(pos, size);

            occupiedArea = int3.Max(pos + size, occupiedArea);
        }

        return positions;

        void AddContainer(int3 pos, int3 size)
        {
            freePositions.Remove(pos);
            placedContainers.Add(new Container(pos, size));

            // add some un-occupied positions, the most optimal positions might not get selected, but we don't need to loop over all positions
            freePositions.AddRange(
            [
                pos + new int3(size.X, 0, 0),
                pos + new int3(0, size.Y, 0),
                pos + new int3(0, 0, size.Z),
                pos + new int3(size.X, size.Y, 0),
                pos + new int3(size.X, 0, size.Z),
                pos + new int3(0, size.Y, size.Z),
                pos + new int3(size.X, size.Y, size.Z),
            ]);
        }
    }

    private static long CalculateArea(int3 size)
        => (long)(size.X * Math.Pow(size.Y, 1.25) * size.Z); // favor x and z over y

    private struct Container
    {
        public int3 Pos;
        public int3 Size;

        public Container(int3 pos, int3 size)
        {
            Pos = pos;
            Size = size;
        }

        public readonly int3 Max => Pos + Size;

        public static bool Intersects(Container a, Container b)
            => (a.Pos.X < b.Max.X && a.Max.X > b.Pos.X) &&
                (a.Pos.Y < b.Max.Y && a.Max.Y > b.Pos.Y) &&
                (a.Pos.Z < b.Max.Z && a.Max.Z > b.Pos.Z);

        public readonly bool IntersectsAny(List<Container> containers)
        {
            for (int i = 0; i < containers.Count; i++)
            {
                if (Intersects(this, containers[i]))
                {
                    return true;
                }
            }

            return false;
        }

        public override readonly string ToString()
            => $"{{Pos: {Pos}, Size: {Size}}}";
    }
}
