// <copyright file="ThrowHelper.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Exceptions;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("FancadeLoaderLib.Editing")]

namespace FancadeLoaderLib.Utils;

#if NETSTANDARD2_1_OR_GREATER
public static class ThrowHelper
#else
internal static class ThrowHelper
#endif
{
	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void ThrowArgumentException(string message, string paramName)
		=> throw new ArgumentException(message, paramName);

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void ThrowArgumentException(string message)
		=> throw new ArgumentException(message);

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void ThrowArgumentOutOfRangeException()
		=> throw new ArgumentOutOfRangeException();

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void ThrowArgumentOutOfRangeException(string paramName)
		=> throw new ArgumentOutOfRangeException(paramName);

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void ThrowArgumentOutOfRangeException(string paramName, string message)
		=> throw new ArgumentOutOfRangeException(paramName, message);

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void ThrowArgumentNullException(string paramName)
		=> throw new ArgumentNullException(paramName);

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void ThrowArgumentNullException(string paramName, string message)
		=> throw new ArgumentNullException(paramName, message);

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void ThrowUnsupportedVersionException(int version)
		=> throw new UnsupportedVersionException(version);

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void ThrowNotImplementedException(string message)
		=> throw new NotImplementedException(message);

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void ThrowInvalidOperationException()
		=> throw new InvalidOperationException();

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void ThrowInvalidOperationException(string message)
		=> throw new InvalidOperationException(message);

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void ThrowNotSupportedException()
		=> throw new NotSupportedException();

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void ThrowInvalidEnumArgumentException(string? argumentName, int invalidValue, Type enumClass)
		=> throw new InvalidEnumArgumentException(argumentName, invalidValue, enumClass);

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void ThrowInvalidDataException(string message)
		=> throw new InvalidDataException(message);
}
