// <copyright file="FcColor.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FancadeLoaderLib;

/// <summary>
/// Represents a fancade color.
/// </summary>
#pragma warning disable CA1028 // Enum Storage should be Int32
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1008:Enums should have zero value", Justification = "Not a valid value.")]
public enum FcColor : byte
#pragma warning restore CA1028 // Enum Storage should be Int32
{
#pragma warning disable SA1602 // Enumeration items should be documented
    LightBrown = 9,
    Brown = 8,
    DarkBrown = 7,
    LightTan = 12,
    Tan = 11,
    DarkTan = 10,
    LightPurple = 30,
    Purple = 29,
    DarkPurple = 28,
    LightPink = 33,
    Pink = 32,
    DarkPink = 31,
    LightRed = 15,
    Red = 14,
    DarkRed = 13,
    LightOrange = 18,
    Orange = 17,
    DarkOrange = 16,
    LightYellow = 21,
    Yellow = 20,
    DarkYellow = 19,
    LightGreen = 24,
    Green = 23,
    DarkGreen = 22,
    LightBlue = 27,
    Blue = 26,
    DarkBlue = 25,
    White = 6,
    Gray1 = 5,
    Gray2 = 4,
    Gray3 = 3,
    Gray4 = 2,
    Black = 1,
#pragma warning restore SA1602 // Enumeration items should be documented
}

/// <summary>
/// Utils for working with <see cref="FcColor"/>.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1649:File name should match first type name", Justification = "Bug in analyzer?")]
public static class FcColorUtils
{
    /// <summary>
    /// The default prefab background color.
    /// </summary>
    public const FcColor DefaultBackgroundColor = FcColor.Blue;
}
