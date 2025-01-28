// <copyright file="InvalidGroupIdException.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System;

namespace FancadeLoaderLib.Exceptions;

/// <summary>
/// Thrown by a prefab group when the prefab's id is invalid and InvalidGroupIdBehaviour is set to <see cref="InvalidGroupIdBehaviour.ThrowException"/>.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "Not desirable for this type.")]
public sealed class InvalidGroupIdException : Exception
{
	/// <summary>
	/// Initializes a new instance of the <see cref="InvalidGroupIdException"/> class.
	/// </summary>
	/// <param name="expectedGroupId">The expected id.</param>
	/// <param name="prefabGroupId">Id of the prefab.</param>
	public InvalidGroupIdException(ushort expectedGroupId, ushort prefabGroupId)
		: base($"Prefab's group id ({prefabGroupId}) is differend than the group's id ({expectedGroupId})")
	{
		ExpectedGroupId = expectedGroupId;
		PrefabGroupId = prefabGroupId;
	}

	/// <summary>
	/// Gets the expected id.
	/// </summary>
	/// <value>The expected id.</value>
	public ushort ExpectedGroupId { get; private set; }

	/// <summary>
	/// Gets the id of the prefab.
	/// </summary>
	/// <value>Id of the prefab.</value>
	public ushort PrefabGroupId { get; private set; }
}
