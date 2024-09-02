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
        public Vector3US FromVoxel; // local position of the connector in SubBlock space
        public Vector3US ToVoxel; // local position of the connector in SubBlock space

        public Connection(Vector3US from, Vector3US to, Vector3US fromVoxel, Vector3US toVoxel)
        {
            From = from;
            To = to;
            FromVoxel = fromVoxel;
            ToVoxel = toVoxel;
        }

        public void Save(FcBinaryWriter writer)
        {
            writer.WriteVec3US(From);
            writer.WriteVec3US(To);
            writer.WriteVec3US(FromVoxel);
            writer.WriteVec3US(ToVoxel);
        }

        public static Connection Load(FcBinaryReader reader)
        {
            Vector3US from = reader.ReadVec3US();
            Vector3US to = reader.ReadVec3US();
            Vector3US fromVoxel = reader.ReadVec3US();
            Vector3US toVoxel = reader.ReadVec3US();

            return new Connection(from, to, fromVoxel, toVoxel);
        }

        public override string ToString()
            => $"[From: {From}, To: {To}, FromVox: {FromVoxel}, ToVox: {ToVoxel}]";
    }
}
