using MathUtils.Vectors;
using System.ComponentModel;

namespace FancadeLoaderLib.Runtime;

internal enum TerminalDirection : byte
{
    PositiveX = 0,
    PositiveZ = 1,
    NegativeX = 2,
    NegativeZ = 3,
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1649:File name should match first type name", Justification = "It funcking does?!")]
internal static class TerminalDirectionUtils
{
    public static ushort3 GetOffset(this TerminalDirection direction)
        => direction switch
        {
            TerminalDirection.PositiveX => new ushort3(1, 0, 0),
            TerminalDirection.PositiveZ => new ushort3(0, 0, 1),
            TerminalDirection.NegativeX => new ushort3(-1, 0, 0),
            TerminalDirection.NegativeZ => new ushort3(0, 0, -1),
            _ => throw new InvalidEnumArgumentException($"{nameof(direction)}", (int)direction, typeof(TerminalDirection)),
        };
}