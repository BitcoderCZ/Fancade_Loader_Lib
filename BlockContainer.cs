using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FancadeLoaderLib
{
    public abstract class BlockContainer
    {
        public BlockData BlockIds;
        public List<BlockValue> BlockValues;
        public List<Connection> Connections;

        protected BlockContainer()
        {
            BlockIds = new BlockData();
            BlockValues = new List<BlockValue>();
            Connections = new List<Connection>();
        }

        protected void save(SaveWriter writer)
        {
            if (BlockIds.Size.X > 0)
            {
                writer.WriteUInt16((ushort)BlockIds.Size.X);
                writer.WriteUInt16((ushort)BlockIds.Size.Y);
                writer.WriteUInt16((ushort)BlockIds.Size.Z);
                for (int z = 0; z < BlockIds.Size.Z; z++)
                    for (int y = 0; y < BlockIds.Size.Y; y++)
                        for (int x = 0; x < BlockIds.Size.X; x++)
                            writer.WriteUInt16(BlockIds.GetSegment(x, y, z));
            }
            if (BlockValues.Count > 0)
            {
                writer.WriteUInt16((ushort)BlockValues.Count);
                for (int i = 0; i < BlockValues.Count; i++)
                    BlockValues[i].Save(writer);
            }
            if (Connections.Count > 0)
            {
                writer.WriteUInt16((ushort)Connections.Count);
                for (int i = 0; i < Connections.Count; i++)
                    Connections[i].Save(writer);
            }
        }

        protected static (BlockData blockIds, List<BlockValue> blockValues, List<Connection> connections) load(SaveReader reader, bool hasBlocks, bool hasValue, bool hasConnections)
        {
            Vector3I size;
            ushort[] blockIds;
            if (hasBlocks)
            {
                size = new Vector3I(reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16());

                blockIds = new ushort[size.X * size.Y * size.Z];
                int i = 0;
                for (int x = 0; x < size.X; x++)
                    for (int y = 0; y < size.Y; y++)
                        for (int z = 0; z < size.Z; z++)
                            blockIds[i++] = reader.ReadUInt16();
            }
            else
            {
                size = Vector3I.Zero;
                blockIds = new ushort[0];
            }

            BlockValue[] values;
            // block values (for number, vec3, probably touch (touch 1,2 or 3), ...)
            if (hasValue)
            {
                values = new BlockValue[reader.ReadUInt16()];
                for (int i = 0; i < values.Length; i++)
                    values[i] = BlockValue.Load(reader);
            }
            else
                values = new BlockValue[0];

            Connection[] connections;
            // connections (between value and variable, ...)
            if (hasConnections)
            {
                connections = new Connection[reader.ReadUInt16()];
                for (int i = 0; i < connections.Length; i++)
                    connections[i] = Connection.Load(reader);
            }
            else
                connections = new Connection[0];

            return (new BlockData(new Array3D<ushort>(blockIds, size.X, size.Y, size.Z)), values.ToList(), connections.ToList());
        }
    }
}
