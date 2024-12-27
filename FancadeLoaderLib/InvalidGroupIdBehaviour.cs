// <copyright file="PrefabGroup.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FancadeLoaderLib;

public enum InvalidGroupIdBehaviour
{
	ThrowException,
	ChangeGroupId,
	CloneAndChangeGroupId,
}
