using BitcoderCZ.Maths.Vectors;
using System.ComponentModel;
using static BitcoderCZ.Fancade.Utils.ThrowHelper;

namespace BitcoderCZ.Fancade.Editing;

/// <summary>
/// Represents the direction of a terminal.
/// </summary>
public enum TerminalDirection : byte
{
    /// <summary>
    /// The terminal is pointing in the +X direction.
    /// </summary>
    PositiveX = 0,

    /// <summary>
    /// The terminal is pointing in the +Z direction.
    /// </summary>
    PositiveZ = 1,

    /// <summary>
    /// The terminal is pointing in the -X direction.
    /// </summary>
    NegativeX = 2,

    /// <summary>
    /// The terminal is pointing in the -Z direction.
    /// </summary>
    NegativeZ = 3,
}

/// <summary>
/// Utils for <see cref="TerminalDirection"/>.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1649:File name should match first type name", Justification = "Bug in the analyzer?")]
public static class TerminalDirectionUtils
{
    /// <summary>
    /// Gets the offset for a given direction.
    /// </summary>
    /// <param name="direction">The <see cref="TerminalDirection"/> to get the offset for.</param>
    /// <returns>The offset for <paramref name="direction"/>.</returns>
    /// <exception cref="InvalidEnumArgumentException">Thrown whem <paramref name="direction"/> is not a valid <see cref="TerminalDirection"/>.</exception>
    public static ushort3 GetOffset(this TerminalDirection direction)
        => direction switch
        {
            TerminalDirection.PositiveX => new ushort3(1, 0, 0),
            TerminalDirection.PositiveZ => new ushort3(0, 0, 1),
            TerminalDirection.NegativeX => new ushort3(-1, 0, 0),
            TerminalDirection.NegativeZ => new ushort3(0, 0, -1),
            _ => throw new InvalidEnumArgumentException($"{nameof(direction)}", (int)direction, typeof(TerminalDirection)),
        };

    /// <summary>
    /// Gets which direction a terminal at the specified position would be facing.
    /// </summary>
    /// <remarks>
    /// Uses which voxels are occupied to determine the direction.
    /// </remarks>
    /// <param name="prefab">The <see cref="Prefab"/> to operate on.</param>
    /// <param name="terminalPosition">Position of the terminal.</param>
    /// <returns>The <see cref="TerminalDirection"/> of the terminal at <paramref name="terminalPosition"/>.</returns>
    public static TerminalDirection GetTerminalDirection(this Prefab prefab, byte3 terminalPosition)
    {
        ThrowIfNull(prefab, nameof(prefab));

        var dir = TerminalDirection.PositiveZ;
        int3 iPos = terminalPosition;

        if (!prefab.GetVoxel(iPos + new int3(0, 0, 1)).IsEmpty)
        {
            dir = TerminalDirection.NegativeX;

            if (!prefab.GetVoxel(iPos + new int3(-1, 0, 0)).IsEmpty)
            {
                dir = prefab.GetVoxel(iPos + new int3(0, 0, -1)).IsEmpty
                    ? TerminalDirection.NegativeZ
                    : TerminalDirection.PositiveX;
            }
        }

        return dir;
    }
}
