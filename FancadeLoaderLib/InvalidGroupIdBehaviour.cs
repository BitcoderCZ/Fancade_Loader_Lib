// <copyright file="InvalidGroupIdBehaviour.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FancadeLoaderLib;

public enum InvalidGroupIdBehaviour
{
	/// <summary>
	/// When the prefab's group is invalid, an exception is thrown.
	/// </summary>
	ThrowException,

	/// <summary>
	/// When the prefab's group is invalid, it is changed to a valid one.
	/// </summary>
	ChangeGroupId,

	/// <summary>
	/// When the prefab's group is invalid, the prefab is cloned and the clone's groups is changed to a valid one.
	/// </summary>
	CloneAndChangeGroupId,
}
