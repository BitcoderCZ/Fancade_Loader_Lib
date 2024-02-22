using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fancade.LevelEditor
{
    public class BlockData
    {
        private const int blockSize = 8;

        public int Length => segments.Length;
        public Vector3I Size { get; private set; }
        private Vector3I maxBlockPos;

        private Array3D<ushort> segments = new Array3D<ushort>(blockSize, blockSize, blockSize);
        public ushort this[int index]
        {
            get => segments[index];
            set => segments[index] = value;
        }

        public BlockData()
        { }
        public BlockData(Array3D<ushort> _segments)
        {
            detectMaxBlockPos();
            ensureSizeAndMaxPos(_segments.LengthX - 1, _segments.LengthY - 1, _segments.LengthZ - 1);
            segments = _segments;
        }

        public bool InBounds(Vector3I pos)
            => segments.InBounds(pos.X, pos.Y, pos.Z);
        public bool InBounds(int x, int y, int z)
            => segments.InBounds(x, y, z);

        public int Index(Vector3I pos)
            => segments.Index(pos);
        public int Index(int x, int y, int z)
            => segments.Index(x, y, z);

        public void SetBlock(int x, int y, int z, Block block)
            => setBlock(x, y, z, block);
        private void setBlock(int x, int y, int z, Block block)
        {
            ushort id = block.MainId;
            Vector3I size = block.GetSize();

            ensureSizeAndMaxPos(x + (size.X - 1), y + (size.Y - 1), z + (size.Z - 1));

            for (int _z = 0; _z < size.Z; _z++)
                for (int _y = 0; _y < size.Y; _y++)
                    for (int _x = 0; _x < size.X; _x++)
                    {
                        Vector3I pos = new Vector3I(_x, _y, _z);
                        if (!block.Blocks.ContainsKey(pos))
                            continue;

                        setSegment(x + _x, y + _y, z + _z, id);
                        id++;
                    }
        }

        public void SetSegment(int x, int y, int z, ushort id)
        {
            if (id > 1)
            {
                ensureSizeAndMaxPos(x, y, z);
                setSegment(x, y, z, id);
            }
            else
            {
                setSegment(x, y, z, id);
                makeSmallerIfRemoved(x, y, z);
            }
        }
        private void setSegment(int x, int y, int z, ushort id)
            => segments[x, y, z] = id;

        public ushort GetSegment(Vector3I pos)
            => GetSegment(pos.X, pos.Y, pos.Z);
        public ushort GetSegment(int x, int y, int z)
            => segments[x, y, z];

        private void ensureSizeAndMaxPos(int x, int y, int z)
        {
            if (x >= segments.LengthX || y >= segments.LengthY || z >= segments.LengthZ)
            {
                segments.Resize(
                    Math.Max(useBlock(x + 1, blockSize), segments.LengthX),
                    Math.Max(useBlock(y + 1, blockSize), segments.LengthY),
                    Math.Max(useBlock(z + 1, blockSize), segments.LengthZ)
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
            segments.Resize(
                useBlock(maxBlockPos.X + 1, blockSize),
                useBlock(maxBlockPos.Y + 1, blockSize),
                useBlock(maxBlockPos.Z + 1, blockSize)
            );
        }

        private void detectMaxBlockPos()
        {
            maxBlockPos = Vector3I.Zero;
            object myLock = new object();

            Parallel.For(0, segments.LengthX, x =>
            {
                for (int y = 0; y < segments.LengthY; y++)
                {
                    for (int z = 0; z < segments.LengthZ; z++)
                    {
                        ushort id = GetSegment(x, y, z);
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

        private int useBlock(int i, int blockSize)
        {
            int mod = i % blockSize;
            return Math.Max(mod == 0 ? i : i + (blockSize - mod), blockSize);
        }
    }
}
