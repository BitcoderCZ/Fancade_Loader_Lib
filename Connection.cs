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
        public Vector3S From;
        public Vector3S To;
        public Vector3S FromConnector; // local position of the connector in SubBlock space
        public Vector3S ToConnector; // local position of the connector in SubBlock space

        public void Save(SaveWriter writer)
        {
            writer.WriteUInt16(From.X);
            writer.WriteUInt16(From.Y);
            writer.WriteUInt16(From.Z);
            writer.WriteUInt16(To.X);
            writer.WriteUInt16(To.Y);
            writer.WriteUInt16(To.Z);
            writer.WriteUInt16(FromConnector.X);
            writer.WriteUInt16(FromConnector.Y);
            writer.WriteUInt16(FromConnector.Z);
            writer.WriteUInt16(ToConnector.X);
            writer.WriteUInt16(ToConnector.Y);
            writer.WriteUInt16(ToConnector.Z);
        }

        public static Connection Load(SaveReader reader)
        {
            Vector3S from = new Vector3S(reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16());
            Vector3S to = new Vector3S(reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16());
            Vector3S fromConnector = new Vector3S(reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16());
            Vector3S toConnector = new Vector3S(reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16());

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
