// <copyright file="IRefValueAction.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace BitcoderCZ.Fancade;

/// <summary>
/// Defines an action that operates on a value passed by reference.
/// </summary>
/// <typeparam name="TArg0">Type of the first argument.</typeparam>
/// <typeparam name="TArg1">Type of the second argument.</typeparam>
public interface IRefValueAction<TArg0, TArg1>
{
    /// <summary>
    /// Invokes the action using the specified value passed by reference.
    /// </summary>
    /// <param name="arg0">The value to operate on.</param>
    /// <param name="arg1">The second argument.</param>
    void Invoke(ref TArg0 arg0, TArg1 arg1);
}