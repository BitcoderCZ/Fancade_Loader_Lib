using MathUtils.Vectors;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace FancadeLoaderLib
{
    public class BlockData
    {
        private const int blockSize = 8;

        public int Length => Array.Length;
        public Vector3I Size { get; private set; }
        private Vector3I maxBlockPos;

        public readonly Array3D<ushort> Array;
        public ushort this[int index]
        {
            get => Array[index];
            set => Array[index] = value;
        }

        public BlockData()
        {
            Array = new Array3D<ushort>(blockSize, blockSize, blockSize);
            maxBlockPos = -Vector3I.One;
        }
        public BlockData(Array3D<ushort> blocks)
        {
            detectMaxBlockPos();
            ensureSizeAndMaxPos(blocks.LengthX - 1, blocks.LengthY - 1, blocks.LengthZ - 1);
            Array = blocks;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool InBounds(Vector3I pos)
            => Array.InBounds(pos.X, pos.Y, pos.Z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool InBounds(int x, int y, int z)
            => Array.InBounds(x, y, z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3I Index(int i)
            => Array.Index(i);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Index(Vector3I pos)
            => Array.Index(pos);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Index(int x, int y, int z)
            => Array.Index(x, y, z);

        //public void SetGroup(Vector3I pos, Block block)
        //    => setGroup(pos.X, pos.Y, pos.Z, block);
        //public void SetBlock(int x, int y, int z, Block block)
        //    => setGroup(x, y, z, block);
        //private void setGroup(int x, int y, int z, Block block)
        //{
        //    ushort id = block.MainId;
        //    Vector3I size = block.GetSize();

        //    ensureSizeAndMaxPos(x + (size.X - 1), y + (size.Y - 1), z + (size.Z - 1));

        //    for (int _z = 0; _z < size.Z; _z++)
        //        for (int _y = 0; _y < size.Y; _y++)
        //            for (int _x = 0; _x < size.X; _x++)
        //            {
        //                Vector3I pos = new Vector3I(_x, _y, _z);
        //                if (!block.Sections.ContainsKey(pos))
        //                    continue;

        //                setPrefab(x + _x, y + _y, z + _z, id);
        //                id++;
        //            }
        //}

        // TODO: make making smaller optional, add EnsureSize(size) and Trim() methods
        public void SetPrefab(Vector3I pos, ushort id)
            => SetPrefab(pos.X, pos.Y, pos.Z, id);
        public void SetPrefab(int x, int y, int z, ushort id)
        {
            if (id > 0)
            {
                ensureSizeAndMaxPos(x, y, z);
                setPrefab(x, y, z, id);
            }
            else
            {
                setPrefab(x, y, z, id);
                makeSmallerIfRemoved(x, y, z);
            }
        }
        private void setPrefab(int x, int y, int z, ushort id)
            => Array[x, y, z] = id;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort GetPrefabId(Vector3I pos)
            => Array[pos.X, pos.Y, pos.Z];
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort GetPrefabId(int x, int y, int z)
            => Array[x, y, z];

        private void ensureSizeAndMaxPos(int x, int y, int z)
        {
            if (x >= Array.LengthX || y >= Array.LengthY || z >= Array.LengthZ)
            {
                Array.Resize(
                    Math.Max(useBlock(x + 1, blockSize), Array.LengthX),
                    Math.Max(useBlock(y + 1, blockSize), Array.LengthY),
                    Math.Max(useBlock(z + 1, blockSize), Array.LengthZ)
                );
            }

            Vector3I size = Size;
            if (x > maxBlockPos.X)
            {
                maxBlockPos.X = x;
                size.X = x + 1;
            }
            if (y > maxBlockPos.Y)
            {
                maxBlockPos.Y = y;
                size.Y = y + 1;
            }
            if (z > maxBlockPos.Z)
            {
                maxBlockPos.Z = z;
                size.Z = z + 1;
            }
            Size = size;
        }
        private void makeSmallerIfRemoved(int x, int y, int z)
        {
            Vector3I pos = new Vector3I(x, y, z);
            if (pos != maxBlockPos)
                return;

            detectMaxBlockPos();
            Array.Resize(
                useBlock(maxBlockPos.X + 1, blockSize),
                useBlock(maxBlockPos.Y + 1, blockSize),
                useBlock(maxBlockPos.Z + 1, blockSize)
            );
        }

        private void detectMaxBlockPos()
        {
            maxBlockPos = -Vector3I.One;
            object myLock = new object();

            Parallel.For(0, Array.LengthX, x =>
            {
                for (int y = 0; y < Array.LengthY; y++)
                {
                    for (int z = 0; z < Array.LengthZ; z++)
                    {
                        ushort id = GetPrefabId(x, y, z);
                        if (id == 0)
                            continue;

                        lock (myLock)
                        {
                            if (z > maxBlockPos.Z)
                                maxBlockPos.Z = z;
                            if (y > maxBlockPos.Y)
                                maxBlockPos.Y = y;
                            if (x > maxBlockPos.X)
                                maxBlockPos.X = x;
                        }
                    }
                }
            });

            Size = maxBlockPos + Vector3I.One;
        }

        public BlockData Clone()
            => new BlockData()
            {
                blocks = this.Array.Clone(),
                Size = this.Size,
                maxBlockPos = this.maxBlockPos,
            };

        private int useBlock(int i, int blockSize)
        {
            int mod = i % blockSize;
            return Math.Max(mod == 0 ? i : i + (blockSize - mod), blockSize);
        }
    }
}
