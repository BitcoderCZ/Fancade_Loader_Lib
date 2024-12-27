// <copyright file="Prefab.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System.Runtime.InteropServices;

namespace FancadeLoaderLib;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct Voxel
{
	//  X
	// -X
	//  Y
	// -Y
	//  Z
	// -Z
	public fixed byte Colors[6];
	public fixed byte NotGlued[6]; // "legos"/glue

	public bool IsEmpty => Colors[0] == 0;

	public override string ToString() =>
		$"[{Colors[0]}, {Colors[1]}, {Colors[2]}, {Colors[3]}, {Colors[4]}, {Colors[5]}; Attribs:" +
		$"{NotGlued[0]}, {NotGlued[1]}, {NotGlued[2]}, {NotGlued[3]}, {NotGlued[4]}, {NotGlued[5]}]";
}
