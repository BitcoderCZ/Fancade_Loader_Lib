// <copyright file="PrefabSegmentMesh.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if !NET8_0_OR_GREATER
using static BitcoderCZ.Fancade.Utils.ThrowHelper;
#endif

namespace BitcoderCZ.Fancade.Runtime.Simulated;

/// <summary>
/// Stores a mesh of a prefab segment.
/// </summary>
public readonly struct PrefabSegmentMesh
{
    private readonly int _voxelCount;
    private readonly Array6<ulong> _bitfields;

    internal PrefabSegmentMesh(int voxelCount, ReadOnlySpan<ulong> bitfields)
    {
        _voxelCount = voxelCount;
        Assign(ref _bitfields, bitfields);
    }

    /// <summary>
    /// Gets the amount of voxels in the mesh.
    /// </summary>
    /// <value>Amount of voxels in the mesh.</value>
    public int VoxelCount => _voxelCount;

    /// <summary>
    /// Gets the bitfield for a side.
    /// </summary>
    /// <param name="sideIndex">Index of the side.</param>
    /// <returns>Bitfield for the side.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong GetSideBitfield(int sideIndex)
    {
#if NET8_0_OR_GREATER
        return _bitfields[sideIndex];
#else
        ThrowIfGreaterThanOrEqualToOrNegative(sideIndex, 6, nameof(sideIndex));

        return Unsafe.Add(ref Unsafe.AsRef(in _bitfields._element0), sideIndex);
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Assign<T>(ref Array6<T> field, ReadOnlySpan<T> param)
    {
        Debug.Assert(param.Length >= 6, $"{nameof(param)} should have at least 6 elements.");

#if NET8_0_OR_GREATER
        field[0] = param[0];
        field[1] = param[1];
        field[2] = param[2];
        field[3] = param[3];
        field[4] = param[4];
        field[5] = param[5];
#else
        field = new Array6<T>(param[0], param[1], param[2], param[3], param[4], param[5]);
#endif
    }

#if NET8_0_OR_GREATER
    [InlineArray(6)]
    private struct Array6<T>
    {
        private T _element0;
    }
#else
    [StructLayout(LayoutKind.Sequential)]
    private struct Array6<T>
    {
        public T _element0;
        public T _element1;
        public T _element2;
        public T _element3;
        public T _element4;
        public T _element5;

        public Array6(T element0, T element1, T element2, T element3, T element4, T element5)
        {
            _element0 = element0;
            _element1 = element1;
            _element2 = element2;
            _element3 = element3;
            _element4 = element4;
            _element5 = element5;
        }
    }
#endif
}