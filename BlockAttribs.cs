﻿using System;

namespace FancadeLoaderLib
{
	public struct BlockAttribs
	{
		public bool Unknown;
		public bool Uneditable;
		public bool IsMultiBlock;
		public bool BlocksInside;
		public bool ValuesInside;
		public bool ConnectionsInside;
		public Collider_T Collider;
		public Type_T Type;

		public bool IsMain => Type != Type_T.Section;

		public static BlockAttribs Default = new BlockAttribs()
		{
			BlocksInside = false,
			Collider = Collider_T.FromEnum(ColliderEnum.Box),
			Type = Type_T.Normal,
			ValuesInside = false,
			IsMultiBlock = false,
			ConnectionsInside = false,
			Uneditable = false
		};
        public static BlockAttribs DefaultSection = new BlockAttribs()
        {
            BlocksInside = false,
            Collider = Collider_T.FromEnum(ColliderEnum.Box),
            Type = Type_T.Section,
            ValuesInside = false,
            IsMultiBlock = false,
            ConnectionsInside = false,
            Uneditable = false
        };

        public void Save(SaveWriter writer, BlockAttribs mainAttribs, string name, bool mainSave)
		{
			if (mainSave)
				writer.WriteUInt8((byte)(
					  (ConnectionsInside ? 0b_0000_0001 : 0)
					| (ValuesInside ? 0b_0000_0010 : 0)
					| (BlocksInside ? 0b_0000_0100 : 0)
					| (IsMultiBlock ? 0b_0001_0000 : 0)
					| (Uneditable ? 0b_0100_0000 : 0)
					| (Unknown ? 0b_1000_0000 : 0)
					| Collider.Value
					));
			else
				writer.WriteUInt8((byte)(
					  0b_0001_0000 //IsMultiBlock should always be true
					| (Uneditable ? 0b_0100_0000 : 0)
					| (Unknown ? 0b_1000_0000 : 0)
					| Collider.Value
					));

			if (!mainSave)
				Type = Type_T.Section; // would break things when loading

			switch (Type)
			{
				case Type_T.Normal:
					writer.WriteUInt8(0x08);
					break;
				case Type_T.Physics:
					writer.WriteUInt8(0x18);
					writer.WriteUInt8(0x01);
					break;
				case Type_T.Script:
					writer.WriteUInt8(0x18);
					writer.WriteUInt8(0x02);
					break;
				case Type_T.Section:
					if (mainAttribs.Type == Type_T.Script)
					{
						writer.WriteUInt8(0x10);
						writer.WriteUInt8(0x02);
					}
					else
						writer.WriteUInt8(0x00);
					break;
			}

			if (mainSave)
				writer.WriteString(name);

			if (mainAttribs.Collider.AddtionalUsed)
				writer.WriteUInt8(mainAttribs.Collider.AdditionalValue);
		}

		public static bool TryLoad(SaveReader reader, bool seek, out BlockAttribs blockAttribs, out string name)
		{
			blockAttribs = default;
			name = "";

			long startPos = reader.Position;

			if (reader.BytesLeft < 2)
				return false;

			byte b1 = reader.ReadUInt8();

			bool unknown = (b1 & 0b_1000_0000) != 0;
			bool uneditable = (b1 & 0b_0100_0000) != 0;
			bool isMultiBlock = (b1 & 0b_0001_0000) != 0;
			bool blocksInside = (b1 & 0b_0000_0100) != 0;
			bool valuesInside = (b1 & 0b_0000_0010) != 0;
			bool connectionsInside = (b1 & 0b_0000_0001) != 0;
			b1 &= 0b_0010_1000;

			Collider_T collider;
			if (b1 == 0x28)
				collider = new Collider_T(b1);
			else if (b1 == 0x08)
				collider = new Collider_T(b1);
			else if (b1 == 0x48)
				collider = new Collider_T(b1);
			else if (b1 == 0x68)
				collider = new Collider_T(b1);
			else
			{
				reader.Position = startPos;
				return false;
			}

			byte b2 = reader.ReadUInt8();
			Type_T type;
			if (b2 == 0x08)
				type = Type_T.Normal;
			else if (b2 == 0x18 && reader.BytesLeft > 0)
			{
				byte b3 = reader.ReadUInt8();
				if (b3 == 0x01)
					type = Type_T.Physics;
				else if (b3 == 0x02)
					type = Type_T.Script;
				else
				{
					reader.Position = startPos;
					return false;
				}
			}
			else if (b2 == 0x00)
			{
				type = Type_T.Section;
				if (collider.AddtionalUsed)
					reader.ReadUInt8();
			}
			else if (b2 == 0x10)
			{
				type = Type_T.Section;
				reader.ReadUInt8();
				if (collider.AddtionalUsed)
					reader.ReadUInt8();
			}
			else
			{
				reader.Position = startPos;
				return false;
			}

			long pos = reader.Position;

			// not sure how reliable this is
			bool isMain = type != Type_T.Section;

			reader.Position = pos;

			name = null;
			if (isMain) { 
				name = reader.ReadString();

				if (collider.AddtionalUsed)
					collider.AdditionalValue = reader.ReadUInt8();
				else if (type == Type_T.Script)
					reader.ReadBytes(1);
			}

			if (seek)
				reader.Position = startPos;

			blockAttribs = new BlockAttribs()
			{
				Unknown = unknown,
				Uneditable = uneditable,
				IsMultiBlock = isMultiBlock,
				ValuesInside = valuesInside,
				ConnectionsInside = connectionsInside,
				BlocksInside = blocksInside,
				Collider = collider,
				Type = type
			};
			return true;
		}

		public override string ToString() => $"[Collider: {Collider}, Type: {Type}]";

		public struct Collider_T
		{
			public byte Value;
			public byte AdditionalValue
			{
				get => additionalValue;
				set
				{
					AdditionalSet = true;
					additionalValue = value;
				}
			}
			private byte additionalValue;
			public bool AdditionalSet { get; private set; }

			public bool AddtionalUsed
			{
				get => (Value & 0b_0010_0000) != 0;
			}

			public Collider_T(byte _value)
			{
				Value = _value;
				additionalValue = 0;
				AdditionalSet = false;
			}

			public static Collider_T FromEnum(ColliderEnum value)
			{
				switch (value)
				{
					case ColliderEnum.Box:
						return new Collider_T(0x08);
					case ColliderEnum.None:
						return new Collider_T(0x28)
						{
							AdditionalValue = 0x00
						};
					case ColliderEnum.Sphere:
						return new Collider_T(0x28)
						{
							AdditionalValue = 0x02
						};
					default:
						throw new ArgumentException($"Cannot convert {value} to Collider_T", "value");
				}
			}

			public ColliderEnum ToEnum()
			{
				if (AddtionalUsed && !AdditionalSet)
					return ColliderEnum.AddtionalRequired;

				if (Value == 0x08)
					return ColliderEnum.Box;
				else if (Value == 0x28)
				{
					if (additionalValue == 0x00)
						return ColliderEnum.None;
					else if (additionalValue == 0x02)
						return ColliderEnum.Sphere;
					else
						return ColliderEnum.Unknown;
				}
				else
					return ColliderEnum.Unknown;
			}

			public override string ToString()
				=> $"{{{ToEnum()}}}";
		}
		public enum ColliderEnum
		{
			None,
			AddtionalRequired,
			Box,
			Sphere,
			Unknown
		}
		public enum Type_T
		{
			Normal,
			Physics,
			Script,
			Section // 0x00
		}
	}
}
