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
    /// <summary>
    /// The LightBrown color.
    /// </summary>
    LightBrown = 9,

    /// <summary>
    /// The Brown color.
    /// </summary>
    Brown = 8,

    /// <summary>
    /// The DarkBrown color.
    /// </summary>
    DarkBrown = 7,

    /// <summary>
    /// The LightTan color.
    /// </summary>
    LightTan = 12,

    /// <summary>
    /// The Tan color.
    /// </summary>
    Tan = 11,

    /// <summary>
    /// The DarkTan color.
    /// </summary>
    DarkTan = 10,

    /// <summary>
    /// The LightPurple color.
    /// </summary>
    LightPurple = 30,

    /// <summary>
    /// The Purple color.
    /// </summary>
    Purple = 29,

    /// <summary>
    /// The DarkPurple color.
    /// </summary>
    DarkPurple = 28,

    /// <summary>
    /// The LightPink color.
    /// </summary>
    LightPink = 33,

    /// <summary>
    /// The Pink color.
    /// </summary>
    Pink = 32,

    /// <summary>
    /// The DarkPink color.
    /// </summary>
    DarkPink = 31,

    /// <summary>
    /// The LightRed color.
    /// </summary>
    LightRed = 15,

    /// <summary>
    /// The Red color.
    /// </summary>
    Red = 14,

    /// <summary>
    /// The DarkRed color.
    /// </summary>
    DarkRed = 13,

    /// <summary>
    /// The LightOrange color.
    /// </summary>
    LightOrange = 18,

    /// <summary>
    /// The Orange color.
    /// </summary>
    Orange = 17,

    /// <summary>
    /// The DarkOrange color.
    /// </summary>
    DarkOrange = 16,

    /// <summary>
    /// The LightYellow color.
    /// </summary>
    LightYellow = 21,

    /// <summary>
    /// The Yellow color.
    /// </summary>
    Yellow = 20,

    /// <summary>
    /// The DarkYellow color.
    /// </summary>
    DarkYellow = 19,

    /// <summary>
    /// The LightGreen color.
    /// </summary>
    LightGreen = 24,

    /// <summary>
    /// The Green color.
    /// </summary>
    Green = 23,

    /// <summary>
    /// The DarkGreen color.
    /// </summary>
    DarkGreen = 22,

    /// <summary>
    /// The LightBlue color.
    /// </summary>
    LightBlue = 27,

    /// <summary>
    /// The Blue color.
    /// </summary>
    Blue = 26,

    /// <summary>
    /// The DarkBlue color.
    /// </summary>
    DarkBlue = 25,

    /// <summary>
    /// The White color.
    /// </summary>
    White = 6,

    /// <summary>
    /// The first Gray color.
    /// </summary>
    Gray1 = 5,

    /// <summary>
    /// The second Gray color.
    /// </summary>
    Gray2 = 4,

    /// <summary>
    /// The third Gray color.
    /// </summary>
    Gray3 = 3,

    /// <summary>
    /// The fourth Gray color.
    /// </summary>
    Gray4 = 2,

    /// <summary>
    /// The Black color.
    /// </summary>
    Black = 1,
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
