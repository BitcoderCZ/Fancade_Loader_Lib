// <copyright file="UnsupportedVersionException.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Raw;
using System;

namespace FancadeLoaderLib.Exceptions;

public class UnsupportedVersionException : Exception
{
	public UnsupportedVersionException(int version)
		: base(version > RawGame.CurrentFileVersion ? $"File version '{version}' isn't supported, highest supported version is {RawGame.CurrentFileVersion}." : $"File version '{version}' isn't supported.")
	{
		Version = version;
	}

	public int Version { get; private set; }
}
