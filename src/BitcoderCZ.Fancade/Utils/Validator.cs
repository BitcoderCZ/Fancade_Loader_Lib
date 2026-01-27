// <copyright file="Validator.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System.Runtime.CompilerServices;
using System.Text;
using static BitcoderCZ.Utils.ThrowHelper;

namespace BitcoderCZ.Fancade.Utils;

internal static class Validator
{
    public static string FancadeString(string value, [CallerArgumentExpression("value")] string valueName = "")
    {
        ThrowIfNull(value, valueName);
        if (!IsAscii(value))
        {
            ThrowArgumentException($"{valueName} must contain only ASCII characters.", valueName);
        }

        if (value.Length > byte.MaxValue)
        {
            ThrowArgumentOutOfRangeException($"{valueName} must not be longer than {byte.MaxValue} characters.", valueName);
        }

        return value;
    }

    public static string FancadeStringNonEmpty(string value, [CallerArgumentExpression("value")] string valueName = "")
    {
        FancadeString(value, valueName);

        if (value.Length is 0)
        {
            ThrowArgumentOutOfRangeException($"{valueName} must not be empty.", valueName);   
        }

        return value;
    }

    private static bool IsAscii(ReadOnlySpan<char> value)
    {
#if NET8_0_OR_GREATER
        return Ascii.IsValid(value);
#else
        foreach (var item in value){
            if (item > 127){
                return false;
            }
        }

        return true;
#endif
    }
}