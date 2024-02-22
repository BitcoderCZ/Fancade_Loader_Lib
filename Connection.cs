using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FancadeLoaderLib
{
    /// <summary>
    /// if From/To.XYZ == 32769 && in block = one side of connection is outside
    /// </summary>
    public struct Connection
    {
        public Vector3I From;
        public Vector3I To;
        public Vector3I FromConnector; // local position of the connector in SubBlock space
        public Vector3I ToConnector; // local position of the connector in SubBlock space

        public void Save(SaveWriter writer)
        {
            writer.WriteUInt16((ushort)From.X);
            writer.WriteUInt16((ushort)From.Y);
            writer.WriteUInt16((ushort)From.Z);
            writer.WriteUInt16((ushort)To.X);
            writer.WriteUInt16((ushort)To.Y);
            writer.WriteUInt16((ushort)To.Z);
            writer.WriteUInt16((ushort)FromConnector.X);
            writer.WriteUInt16((ushort)FromConnector.Y);
            writer.WriteUInt16((ushort)FromConnector.Z);
            writer.WriteUInt16((ushort)ToConnector.X);
            writer.WriteUInt16((ushort)ToConnector.Y);
            writer.WriteUInt16((ushort)ToConnector.Z);
        }

        public static Connection Load(SaveReader reader)
        {
            Vector3I from = new Vector3I(reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16());
            Vector3I to = new Vector3I(reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16());
            Vector3I fromConnector = new Vector3I(reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16());
            Vector3I toConnector = new Vector3I(reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16());

            return new Connection()
            {
                From = from,
                To = to,
                FromConnector = fromConnector,
                ToConnector = toConnector
            };
        }

        public override string ToString()
            => $"[From: {From}, To: {To}, FromCon.: {FromConnector}, ToCon.: {ToConnector}]";
    }
}
