// <copyright file="EditorScriptBlockBuilder.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Utils;
using MathUtils.Vectors;
using System;
using System.Buffers.Binary;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text;

namespace FancadeLoaderLib.Editing.Scripting.Builders;

public sealed class EditorScriptBlockBuilder : BlockBuilder
{
	/*
	Non minimized code:
	function con(nChr) {
		return nChr > 64 && nChr < 91
		? nChr - 65
		: nChr > 96 && nChr < 123
		? nChr - 71
		: nChr > 47 && nChr < 58
		? nChr + 4
		: nChr === 43
		? 62
		: nChr === 47
		? 63
		: 0;
	}
	function b64d(s, nBlocksSize) {
		const en = s.replace(/[^A-Za-z0-9+/]/g, "");
		const inL = en.length;
		const outL = nBlocksSize
		? Math.ceil(((inL * 3 + 1) >> 2) / nBlocksSize) * nBlocksSize
		: (inL * 3 + 1) >> 2;
		const bs = new Uint8Array(outL);
		let m3;
		let m4;
		let nUint24 = 0;
		let outIdx = 0;
		for (let nInIdx = 0; nInIdx < inL; nInIdx++) {
			m4 = nInIdx & 3;
			nUint24 |= con(en.charCodeAt(nInIdx)) << (6 * (3 - m4));
			if (m4 === 3 || inL - nInIdx === 1) {
				m3 = 0;
				while (m3 < 3 && outIdx < outL) {
					bs[outIdx] = (nUint24 >>> ((16 >>> m3) & 24)) & 255;
					m3++;
					outIdx++;
				}
				nUint24 = 0;
			}
		}
		return bs;
	}
	var c = "[CODE]";
	var bytes = b64d(c);
	var view = new DataView(bytes.buffer);
	var encoder = new TextDecoder();
	clearLog();
	var i = 0;
	var setLL = view.getInt32(i, true);
	log("Numb blocks: " + setLL);
	i+=4;
	for(var j=0; j < setLL; j++) {
		var x = view.getInt32(i, true);
		var y = view.getInt32(i+4, true);
		var z = view.getInt32(i+8, true);
		var bId = view.getInt32(i+12, true);
		i += 16;
		log("Set block: id: " + bId + " pos: x:" + x + " y: " + y + " z: " + z);
		setBlock(x, y, z, bId);
	}
	log("Set blocks");
	updateChanges();
	log("Updated changes")
	var setVLL = view.getInt32(i, true);
	log("Numb values: " + setVLL);
	i+=4;
	for(var j=0; j < setVLL; j++) {
		let x = view.getInt32(i, true);
		let y = view.getInt32(i+4, true);
		let z = view.getInt32(i+8, true);
		let vIndex = view.getInt32(i+12, true);
		let vt = view.getInt32(i+16, true);
		i += 20;
		var v;
		if (vt == 0){
			v = view.getFloat32(i, true);
			i +=4;
		} else if (vt == 1) {
			var vl = view.getInt32(i, true);
			i +=4;
			var bs = bytes.subarray(i, i + vl);
			i += vl;
			v = encoder.decode(bs);
		} else if (vt == 2) {
			v = [view.getFloat32(i, true),view.getFloat32(i+4, true),view.getFloat32(i+8, true)];
			i += 12;
		}
		log("Set value: index: " + vIndex + " type: " + vt + " pos: x:" + x + " y: " + y + " z: " + z);
		setBlockValue(x, y, z, vIndex, v);
	}
	log("Set values");
	updateChanges();
	log("Updated changes");
	var cLL = view.getInt32(i, true);
	log("Numb connections: " + cLL);
	i+=4;
	for(var j=0; j < cLL; j++) {
		var x1 = view.getInt32(i, true);
		var y1 = view.getInt32(i+4, true);
		var z1 = view.getInt32(i+8, true);
		var x2 = view.getInt32(i+12, true);
		var y2 = view.getInt32(i+16, true);
		var z2 = view.getInt32(i+20, true);
		var ti1 = view.getInt32(i+24, true);
		var ti2 = view.getInt32(i+28, true);
		i += 32;
		log("connect, from: " + x1 + ", " + y1 + ", " + z1 + " from index: " + ti1 + " to: " + x2 + ", " + y2 + ", " + z2 + " to index: " + ti2)
		connect(x1, y1, z1, ti1, x2, y2, z2, ti2);
	}
	log("Connected");
	updateChanges();
	log("Updated changes, done");
	 */
	private const string Base64Code = """
		var t=function(t,e){const n=t.replace(/[^A-Za-z0-9+/]/g,""),a=n.length,g=e?Math.ceil((3*a+1>>2)/e)*e:3*a+1>>2,r=new Uint8Array(g);let I,o,l=0,c=0;for(let t=0;t<a;t++)if(o=3&t,l|=((f=n.charCodeAt(t))>64&&f<91?f-65:f>96&&f<123?f-71:f>47&&f<58?f+4:43===f?62:47===f?63:0)<<6*(3-o),3===o||a-t==1){for(I=0;I<3&&c<g;)r[c]=l>>>(16>>>I&24)&255,I++,c++;l=0}var f;return r}("[CODE]"),e=new DataView(t.buffer),n=new TextDecoder,a=0,g=e.getInt32(a,!0);a+=4;for(var r=0;r<g;r++){var I=e.getInt32(a,!0),o=e.getInt32(a+4,!0),l=e.getInt32(a+8,!0),c=e.getInt32(a+12,!0);a+=16,setBlock(I,o,l,c)}updateChanges();var f=e.getInt32(a,!0);a+=4;for(r=0;r<f;r++){let g=e.getInt32(a,!0),r=e.getInt32(a+4,!0),I=e.getInt32(a+8,!0),o=e.getInt32(a+12,!0),l=e.getInt32(a+16,!0);var v;if(a+=20,0==l)v=e.getFloat32(a,!0),a+=4;else if(1==l){var s=e.getInt32(a,!0);a+=4;var u=t.subarray(a,a+s);a+=s,v=n.decode(u)}else 2==l&&(v=[e.getFloat32(a,!0),e.getFloat32(a+4,!0),e.getFloat32(a+8,!0)],a+=12);setBlockValue(g,r,I,o,v)}updateChanges();var d=e.getInt32(a,!0);a+=4;for(r=0;r<d;r++){var i=e.getInt32(a,!0),h=e.getInt32(a+4,!0),C=e.getInt32(a+8,!0),p=e.getInt32(a+12,!0),w=e.getInt32(a+16,!0),F=e.getInt32(a+20,!0),A=e.getInt32(a+24,!0),D=e.getInt32(a+28,!0);a+=32,connect(i,h,C,A,p,w,F,D)}updateChanges();
		""";

	public enum CompressionType
	{
		None,
		Base64,
	}

	public CompressionType Compression { get; init; } = CompressionType.Base64;

#if NET5_0_OR_GREATER
	public override string Build(int3 buildPos)
#else
	public override object Build(int3 buildPos)
#endif
	{
		Block[] blocks = PreBuild(buildPos, sortByPos: true); // sortByPos is requred because of a bug that sometimes deletes objects if they are placed from +Z to -Z, even if they aren't overlaping

		return Compression switch
		{
			CompressionType.None => BuildRaw(blocks),
			CompressionType.Base64 => BuildBase64(blocks),
			_ => throw new InvalidEnumArgumentException(nameof(Compression), (int)Compression, typeof(CompressionType)),
		};
	}

	private string BuildRaw(Block[] blocks)
	{
		StringBuilder builder = new StringBuilder();

		if (blocks.Length > 0 && blocks[0].Position != int3.Zero)
		{
			builder.AppendLine($"setBlock(0,0,0,1);"); // make sure the level origin doesn't shift
		}

		for (int i = 0; i < blocks.Length; i++)
		{
			Block block = blocks[i];
#if NET6_0_OR_GREATER
			builder.AppendLine(CultureInfo.InvariantCulture, $"setBlock({block.Position.X},{block.Position.Y},{block.Position.Z},{block.Type.Prefab.Id});");
#else
			builder.AppendLine($"setBlock({block.Position.X},{block.Position.Y},{block.Position.Z},{block.Type.Prefab.Id});");
#endif
		}

		builder.AppendLine("updateChanges();");

		for (int i = 0; i < settings.Count; i++)
		{
			SettingRecord set = settings[i];

			string val = set.Value switch
			{
				null => "null",
				byte b => b.ToString(CultureInfo.InvariantCulture),
				ushort s => s.ToString(CultureInfo.InvariantCulture),
				float f => f.ToString(CultureInfo.InvariantCulture),
				string s => $"\"{s}\"",
				float3 v => $"[{v.X.ToString(CultureInfo.InvariantCulture)},{v.Y.ToString(CultureInfo.InvariantCulture)},{v.Z.ToString(CultureInfo.InvariantCulture)}]",
				Rotation r => $"[{r.Value.X.ToString(CultureInfo.InvariantCulture)},{r.Value.Y.ToString(CultureInfo.InvariantCulture)},{r.Value.Z.ToString(CultureInfo.InvariantCulture)}]",
				_ => throw new InvalidDataException($"Object of type '{set.Value.GetType()}' isn't a valid setting value."),
			};

#if NET6_0_OR_GREATER
			builder.AppendLine(CultureInfo.InvariantCulture, $"setBlockValue({set.Block.Position.X},{set.Block.Position.Y},{set.Block.Position.Z},{set.ValueIndex},{val});");
#else
			builder.AppendLine($"setBlockValue({set.Block.Position.X},{set.Block.Position.Y},{set.Block.Position.Z},{set.ValueIndex},{val});");
#endif
		}

		builder.AppendLine("updateChanges();");

		for (int i = 0; i < connections.Count; i++)
		{
			ConnectionRecord con = connections[i];
			int3 from = con.From.BlockPosition;
			int fromTerminal = con.From.TerminalIndex;
			int3 to = con.To.BlockPosition;
			int toTerminal = con.To.TerminalIndex;
#if NET6_0_OR_GREATER
			builder.AppendLine(CultureInfo.InvariantCulture, $"connect({from.X},{from.Y},{from.Z},{fromTerminal},{to.X},{to.Y},{to.Z},{toTerminal});");
#else
			builder.AppendLine($"connect({from.X},{from.Y},{from.Z},{fromTerminal},{to.X},{to.Y},{to.Z},{toTerminal});");
#endif
		}

		builder.AppendLine("updateChanges();");

		return builder.ToString();
	}

	private string BuildBase64(Block[] blocks)
	{
		byte[] bufer;
		using (MemoryStream stream = new MemoryStream())
		using (EditorScriptBase64Writer writer = new EditorScriptBase64Writer(stream))
		{
			bool insertBlockAtZero = blocks.Length > 0 && blocks[0].Position != int3.Zero;

			writer.WriteInt32(blocks.Length + (insertBlockAtZero ? 1 : 0));

			if (insertBlockAtZero)
			{
				// make sure the level origin doesn't shift
				writer.WriteInt32(0);
				writer.WriteInt32(0);
				writer.WriteInt32(0);
				writer.WriteInt32(1);
			}

			for (int i = 0; i < blocks.Length; i++)
			{
				Block block = blocks[i];
				writer.WriteInt32(block.Position.X);
				writer.WriteInt32(block.Position.Y);
				writer.WriteInt32(block.Position.Z);
				writer.WriteInt32(block.Type.Prefab.Id);
			}

			writer.WriteInt32(settings.Count);
			for (int i = 0; i < settings.Count; i++)
			{
				SettingRecord set = settings[i];
				writer.WriteInt32(set.Block.Position.X);
				writer.WriteInt32(set.Block.Position.Y);
				writer.WriteInt32(set.Block.Position.Z);
				writer.WriteInt32(set.ValueIndex);

				if (set.Value is byte numB)
				{
					writer.WriteInt32(0);
					writer.WriteSingle(numB); // javascript only has float type, so no reason to add a value type (other than saving space)
				}
				else if (set.Value is ushort numS)
				{
					writer.WriteInt32(0);
					writer.WriteSingle(numS); // javascript only has float type, so no reason to add a value type (other than saving space)
				}
				else if (set.Value is float numF)
				{
					writer.WriteInt32(0);
					writer.WriteSingle(numF);
				}
				else if (set.Value is string s)
				{
					writer.WriteInt32(1);
					writer.WriteString(s);
				}
				else if (set.Value is float3 v3)
				{
					writer.WriteInt32(2);
					writer.WriteSingle(v3.X);
					writer.WriteSingle(v3.Y);
					writer.WriteSingle(v3.Z);
				}
				else if (set.Value is Rotation rot)
				{
					writer.WriteInt32(2);
					writer.WriteSingle(rot.Value.X);
					writer.WriteSingle(rot.Value.Y);
					writer.WriteSingle(rot.Value.Z);
				}
				else if (set.Value is bool b)
				{
					writer.WriteInt32(3);
					writer.WriteInt32(b ? 1 : 0);
				}
				else
				{
					ThrowHelper.ThrowInvalidDataException($"Object of type '{set.Value?.GetType()?.FullName ?? "null"}' isn't a valid setting value.");
				}
			}

			writer.WriteInt32(connections.Count);
			for (int i = 0; i < connections.Count; i++)
			{
				ConnectionRecord con = connections[i];
				int3 from = con.From.BlockPosition;
				int fromTerminal = con.From.TerminalIndex;
				int3 to = con.To.BlockPosition;
				int toTerminal = con.To.TerminalIndex;
				writer.WriteInt32(from.X);
				writer.WriteInt32(from.Y);
				writer.WriteInt32(from.Z);
				writer.WriteInt32(to.X);
				writer.WriteInt32(to.Y);
				writer.WriteInt32(to.Z);
				writer.WriteInt32(fromTerminal);
				writer.WriteInt32(toTerminal);
			}

			bufer = new byte[stream.Length];
			stream.Position = 0;
			stream.Read(bufer, 0, bufer.Length);
		}

		string code = Convert.ToBase64String(bufer);
		return Base64Code.Replace("[CODE]", code, StringComparison.Ordinal);
	}

	private class EditorScriptBase64Writer : IDisposable
	{
		private readonly Stream _stream;

		public EditorScriptBase64Writer(byte[] bytes)
		{
			_stream = new MemoryStream(bytes);

			Position = 0;
		}

		public EditorScriptBase64Writer(Stream stream)
		{
			if (!stream.CanWrite)
			{
				ThrowHelper.ThrowArgumentException($"{nameof(stream)} isn't writeable.", nameof(stream));
			}

			_stream = stream;

			Position = 0;
		}

		public EditorScriptBase64Writer(string path)
		{
			_stream = new FileStream(path, FileMode.Create, FileAccess.Write);

			Position = 0;
		}

		public long Position { get => _stream.Position; set => _stream.Position = value; }

		public long Length => _stream.Length;

		public void Reset()
			=> _stream.Position = 0;

		public void WriteSpan(ReadOnlySpan<byte> bytes)
			=> _stream.Write(bytes);

		public void WriteInt32(int value)
		{
			Span<byte> buffer = stackalloc byte[sizeof(int)];
			BinaryPrimitives.WriteInt32LittleEndian(buffer, value);
			WriteSpan(buffer);
		}

		public void WriteSingle(float value)
		{
			Span<byte> buffer = stackalloc byte[sizeof(float)];
			BinaryPrimitives.WriteUInt32LittleEndian(buffer, UnsafeUtils.BitCast<float, uint>(value));
			WriteSpan(buffer);
		}

		public void WriteString(string value)
		{
			if (value.Length > ushort.MaxValue)
			{
				ThrowHelper.ThrowArgumentException($"{nameof(value)} is longer than the maximum allowed string length ({ushort.MaxValue}).", nameof(value));
			}

			byte[] bytes = Encoding.UTF8.GetBytes(value);
			WriteInt32((ushort)bytes.Length);
			WriteSpan(bytes);
		}

		public void Flush()
			=> _stream.Flush();

		public void Dispose()
		{
			_stream.Close();
			_stream.Dispose();
		}
	}
}
