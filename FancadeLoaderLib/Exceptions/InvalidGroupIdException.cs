// <copyright file="InvalidGroupIdException.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System;

namespace FancadeLoaderLib.Exceptions;

public class InvalidGroupIdException : Exception
{
	public InvalidGroupIdException(ushort expectedGroupId, ushort prefabGroupId)
		: base($"Prefab's group id ({prefabGroupId}) is differend than the group's id ({expectedGroupId})")
	{
		ExpectedGroupId = expectedGroupId;
		PrefabGroupId = prefabGroupId;
	}

	public ushort ExpectedGroupId { get; private set; }

	public ushort PrefabGroupId { get; private set; }
}
