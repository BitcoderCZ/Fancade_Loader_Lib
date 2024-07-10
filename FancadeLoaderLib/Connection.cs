using MathUtils.Vectors;

namespace FancadeLoaderLib
{
    /// <remarks>
    /// if From/To.XYZ == 32769 AND in block -> one side of connection is outside
    /// </remarks>
    public struct Connection
    {
        public Vector3US From;
        public Vector3US To;
        public Vector3US FromConnector; // local position of the connector in SubBlock space
        public Vector3US ToConnector; // local position of the connector in SubBlock space

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
            Vector3US from = new Vector3US(reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16());
            Vector3US to = new Vector3US(reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16());
            Vector3US fromConnector = new Vector3US(reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16());
            Vector3US toConnector = new Vector3US(reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16());

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
