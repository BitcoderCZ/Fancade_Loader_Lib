namespace FancadeLoaderLib
{
    public class Level : BlockContainer
    {
        private bool unknownFlag; // aditional byte after the color (so far only seen 0, in ECVV_SET_ANG_LIMITS_E)

        private string name;
        public string Name
        {
            get => name;
            set
            {
                ArgumentNullException.ThrowIfNull(value, nameof(value));
                name = value;
            }
        }
        public FancadeColor BackgroundColor;
        public bool LevelUnEditable;

        public Level(string _name)
            : base()
        {
            ArgumentNullException.ThrowIfNull(_name, nameof(_name));
            name = _name;
            BackgroundColor = FancadeColorE.Default;
            LevelUnEditable = false;
        }

        public void Save(SaveWriter writer)
        {
            writer.WriteUInt8((byte)(
                (LevelUnEditable ? 0b_0100_0000 : 0) |
                (unknownFlag ? 0b_0100_0000 : 0) |
                (BlockIds.Size.X > 0 ? 0b_0100 : 0) |
                (BlockValues.Count > 0 ? 0b_0010 : 0) |
                (Connections.Count > 0 ? 0b_0001 : 0)
            ));
            writer.WriteUInt8((byte)(BackgroundColor == FancadeColorE.Default ? 0x18 : 0x19));
            writer.WriteUInt8(0x03);
            writer.WriteString(Name);
            if (BackgroundColor != FancadeColorE.Default)
                writer.WriteUInt8((byte)BackgroundColor);

            if (unknownFlag)
                writer.WriteUInt8(0);

            save(writer);
        }

        public static Level Load(SaveReader reader)
        {
            reader.NextThing(false, out object _info);
            byte[] info = (byte[])_info;

            string name = reader.ReadString();

            FancadeColor backgroundColor = FancadeColorE.Default;
            if ((info[1] & 0b_0000_0001) != 0)
                backgroundColor = (FancadeColor)reader.ReadUInt8();

            if ((info[0] & 0b_0010_0000) != 0)
                reader.ReadUInt8();

            var (blockIds, values, connections) = load(reader, hasBlocks: (info[0] & 0b_0000_0100) != 0, hasValues: (info[0] & 0b_0000_0010) != 0, hasConnections: (info[0] & 0b_0000_0001) != 0);

            return new Level(name)
            {
                unknownFlag = (info[0] & 0b_0010_0000) != 0,
                BackgroundColor = backgroundColor,
                LevelUnEditable = (info[0] & 0b_0100_0000) != 0,
                BlockIds = blockIds,
                BlockValues = values,
                Connections = connections
            };
        }

        public override string ToString() => $"[{Name}, Size: {BlockIds.Size}]";
    }
}
