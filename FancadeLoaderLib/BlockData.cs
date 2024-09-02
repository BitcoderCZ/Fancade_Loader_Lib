using MathUtils.Vectors;
using System;
using System.Runtime.CompilerServices;

namespace FancadeLoaderLib
{
    public class BlockData
    {
        private const int blockSize = 8;

        public int Length => Array.Length;
        public Vector3I Size { get; private set; }

        public readonly Array3D<ushort> Array;
        public ushort this[int index]
        {
            get => Array[index];
            set => Array[index] = value;
        }

        public BlockData()
        {
            Array = new Array3D<ushort>(blockSize, blockSize, blockSize);
        }
        public BlockData(Array3D<ushort> blocks)
        {
            Array = blocks;

            Size = new Vector3I(Array.LengthX, Array.LengthY, Array.LengthZ);
            Trim();
        }
        public BlockData(BlockData data)
        {
            Array = data.Array.Clone();
            Size = data.Size;
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

        public void SetGroup(Vector3I pos, PrefabGroup group)
            => setGroup(pos.X, pos.Y, pos.Z, group);
        public void SetBlock(int x, int y, int z, PrefabGroup group)
            => setGroup(x, y, z, group);
        private void setGroup(int x, int y, int z, PrefabGroup group)
        {
            ushort id = group.Id;
            Vector3B size = group.Size;

            EnsureSize(x + (size.X - 1), y + (size.Y - 1), z + (size.Z - 1));

            for (byte _z = 0; _z < size.Z; _z++)
                for (byte _y = 0; _y < size.Y; _y++)
                    for (byte _x = 0; _x < size.X; _x++)
                    {
                        Vector3B pos = new Vector3B(_x, _y, _z);
                        if (!group.ContainsKey(pos))
                            continue;

                        setBlock(x + _x, y + _y, z + _z, id);
                        id++;
                    }
        }

        #region SetBlock
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetBlock(Vector3I pos, ushort id)
            => SetBlock(pos.X, pos.Y, pos.Z, id);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetBlock(int index, ushort id)
        {
            Vector3I pos = Index(index);
            SetBlock(pos.X, pos.Y, pos.Z, id);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetBlock(int x, int y, int z, ushort id)
        {
            if (id != 0)
                EnsureSize(x, y, z); // not placing "air"

            setBlock(x, y, z, id);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetBlockUnchecked(Vector3I pos, ushort id)
            => SetBlockUnchecked(pos.X, pos.Y, pos.Z, id);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetBlockUnchecked(int index, ushort id)
        {
            Vector3I pos = Index(index);
            SetBlockUnchecked(pos.X, pos.Y, pos.Z, id);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetBlockUnchecked(int x, int y, int z, ushort id)
            => setBlock(x, y, z, id);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void setBlock(int x, int y, int z, ushort id)
            => Array[x, y, z] = id;
        #endregion

        #region GetBlock
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort GetBlock(Vector3I pos)
            => Array[pos.X, pos.Y, pos.Z];
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort GetBlock(int x, int y, int z)
            => Array[x, y, z];
        #endregion

        public void Trim()
        {
            if (Size == Vector3I.Zero)
                return;

            int maxX = int.MaxValue;
            int maxY = int.MaxValue;
            int maxZ = int.MaxValue;

            Vector3I scanPos = Size - Vector3I.One;

            while (true)
            {
                if (maxX == int.MaxValue)
                {
                    for (int y = 0; y <= scanPos.Y; y++)
                    {
                        for (int z = 0; z <= scanPos.Z; z++)
                        {
                            if (Array[Index(scanPos.X, y, z)] != 0)
                            {
                                maxX = scanPos.X;
                                goto endX;
                            }
                        }
                    }
                }
            endX:

                if (maxY == int.MaxValue)
                {
                    for (int x = 0; x <= scanPos.X; x++)
                    {
                        for (int z = 0; z <= scanPos.Z; z++)
                        {
                            if (Array[Index(x, scanPos.Y, z)] != 0)
                            {
                                maxY = scanPos.Y;
                                goto endY;
                            }
                        }
                    }
                }
            endY:

                if (maxZ == int.MaxValue)
                {
                    for (int x = 0; x <= scanPos.X; x++)
                    {
                        for (int y = 0; y <= scanPos.Y; y++)
                        {
                            if (Array[Index(x, y, scanPos.Z)] != 0)
                            {
                                maxZ = scanPos.Z;
                                goto endZ;
                            }
                        }
                    }
                }
            endZ:

                if (maxX != int.MaxValue && maxY != int.MaxValue && maxZ != int.MaxValue)
                {
                    resize(new Vector3I(maxX + 1, maxY + 1, maxZ + 1));
                    return;
                }
                else if (scanPos.X == 1 && scanPos.Y == 1 && scanPos.Z == 1)
                {
                    // no blocks
                    resize(Vector3I.Zero);
                    return;
                }

                scanPos = new Vector3I(Math.Max(1, scanPos.X - 1), Math.Max(1, scanPos.Y - 1), Math.Max(1, scanPos.Z - 1));
            }
        }

        public void EnsureSize(Vector3I pos)
            => EnsureSize(pos.X, pos.Y, pos.Z);
        public void EnsureSize(int posX, int posY, int posZ)
        {
            Vector3I size = Size;
            if (posX >= size.X)
                size.X = posX + 1;
            if (posY >= size.Y)
                size.Y = posY + 1;
            if (posZ >= size.Z)
                size.Z = posZ + 1;

            if (size != Size)
            {
                // only resize if actually needed
                if (size.X > Array.LengthX || size.Y > Array.LengthY || size.Z > Array.LengthZ)
                    resize(size);
                else
                    Size = size;
            }
        }

        public BlockData Clone()
            => new BlockData(this);

        #region Utils
        private void resize(Vector3I size)
        {
            Array.Resize(
                useBlock(size.X, blockSize),
                useBlock(size.Y, blockSize),
                useBlock(size.Z, blockSize)
            );

            Size = size;
        }

        private int useBlock(int numb, int blockSize)
        {
            int mod = numb % blockSize;
            return Math.Max(mod == 0 ? numb : numb + (blockSize - mod), blockSize);
        }
        #endregion
    }
}
