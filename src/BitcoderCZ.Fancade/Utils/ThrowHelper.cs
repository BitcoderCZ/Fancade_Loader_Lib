// <copyright file="ThrowHelper.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Fancade.Exceptions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("BitcoderCZ.Fancade.Editing")]
[assembly: InternalsVisibleTo("BitcoderCZ.Fancade.Runtime")]

namespace BitcoderCZ.Fancade.Utils;

internal static class ThrowHelper
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
    public static void ThrowNotImplementedException()
        => throw new NotImplementedException();

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

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowKeyNotFound<TKey>(TKey key)
        => throw new KeyNotFoundException($"Key '{key}' wasn't found.");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowDuplicateKey<TKey>(TKey key)
        => throw new ArgumentException($"Duplicate key '{key}'.", nameof(key));

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowIndexArgumentOutOfRange()
        => throw new IndexOutOfRangeException();

    #region Conditional
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNegative(int value, [CallerArgumentExpression("value")] string paramName = "")
    {
        if (value < 0)
        {
            ThrowArgumentOutOfRangeException(paramName, $"{paramName} cannot be negative.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfLessThan(int value, int other, [CallerArgumentExpression("value")] string paramName = "")
    {
        if (value.CompareTo(other) < 0)
        {
            ThrowArgumentOutOfRangeException(paramName, $"{paramName} ({value}) must be greater than or equal {other}.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfGreaterThan(int value, int other, [CallerArgumentExpression("value")] string paramName = "")
    {
        if (value.CompareTo(other) > 0)
        {
            ThrowArgumentOutOfRangeException(paramName, $"{paramName} ({value}) must be less than or equal to {other}.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNull<T>(T? value, [CallerArgumentExpression("value")] string paramName = "")
    {
        if (value is null)
        {
            ThrowArgumentOutOfRangeException(paramName, $"{paramName} cannot be null.");
        }
    }
    #endregion

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowConcurrentOperation()
        => throw new InvalidOperationException("Operations that change non-concurrent collections must have exclusive access. A concurrent update was performed on this collection and corrupted its state. The collection's state is no longer correct.");

    internal static void ThrowVersionCheckFailed()
        => throw new InvalidOperationException("Collection was modified after the enumerator was instantiated.");
}
