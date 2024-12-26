using MathUtils.Vectors;

namespace FancadeLoaderLib
{
	/// <remarks>
	/// if From/To.XYZ == 32769 AND in block -> one side of connection is outside
	/// </remarks>
	public struct Connection
	{
		public ushort3 From;
		public ushort3 To;
		public ushort3 FromVoxel; // local position of the connector in SubBlock space
		public ushort3 ToVoxel; // local position of the connector in SubBlock space

		public Connection(ushort3 from, ushort3 to, ushort3 fromVoxel, ushort3 toVoxel)
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
			ushort3 from = reader.ReadVec3US();
			ushort3 to = reader.ReadVec3US();
			ushort3 fromVoxel = reader.ReadVec3US();
			ushort3 toVoxel = reader.ReadVec3US();

			return new Connection(from, to, fromVoxel, toVoxel);
		}

		public override string ToString()
			=> $"[From: {From}, To: {To}, FromVox: {FromVoxel}, ToVox: {ToVoxel}]";
	}
}
