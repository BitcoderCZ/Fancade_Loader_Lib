// <copyright file="WireType.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FancadeLoaderLib.Editing;

public enum WireType
{
	Error = 0,
	Void = 1,
	Float = 2,
	FloatPtr = 3,
	Vec3 = 4,
	Vec3Ptr = 5,
	Rot = 6,
	RotPtr = 7,
	Bool = 8,
	BoolPtr = 9,
	Obj = 10,
	ObjPtr = 11,
	Con = 12,
	ConPtr = 13,
}

#pragma warning disable SA1649 // File name should match first type name - it fucking does???
public static class WireTypeUtils
#pragma warning restore SA1649
{
	public static WireType ToPointer(this WireType wireType)
		=> wireType == WireType.Error ? wireType : (WireType)((int)wireType | 1);

	public static WireType ToNotPointer(this WireType wireType)
		=> wireType == WireType.Void ? wireType : (WireType)((int)wireType & (int.MaxValue ^ 1));

	public static bool IsPointer(this WireType wireType)
		=> wireType != WireType.Void && ((int)wireType & 1) == 1;
}