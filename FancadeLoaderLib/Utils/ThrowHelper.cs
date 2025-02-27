// <copyright file="ThrowHelper.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace FancadeLoaderLib.Utils;

internal static class ThrowHelper
{
	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void ThrowArgumentOutOfRange()
		=> throw new ArgumentOutOfRangeException();

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void ThrowArgumentOutOfRange(string paramName)
		=> throw new ArgumentOutOfRangeException(paramName);

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void ThrowArgumentNull(string paramName)
		=> throw new ArgumentNullException(paramName);

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void ThrowArgumentNull(string paramName, string message)
		=> throw new ArgumentNullException(paramName, message);
}
