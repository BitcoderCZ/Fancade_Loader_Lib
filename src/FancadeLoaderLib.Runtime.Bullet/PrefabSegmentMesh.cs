using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FancadeLoaderLib.Runtime.Bullet;

public sealed class PrefabSegmentMesh
{
    private readonly int _voxelCount;
    private readonly Array6<ulong> _bitfields;
    private readonly Array6<int> _lengths0;
    private readonly Array6<int> _lengths1;
    private readonly Array6<Vector3[]> _verts0;
    private readonly Array6<Vector3[]> _verts1;
    private readonly Array6<Vector3[]> _verts2;
    private readonly Array6<Vector3[]> _verts3;
    private readonly Array6<Vector2[]> _uvs0;
    private readonly Array6<Vector2[]> _uvs1;
    private readonly Array6<Vector2[]> _uvs2;
    private readonly Array6<Vector2[]> _uvs3;

    internal PrefabSegmentMesh(int voxelCount, ReadOnlySpan<ulong> bitfields, ReadOnlySpan<int> lengths0, ReadOnlySpan<int> lengths1, ReadOnlySpan<Vector3[]> verts0, ReadOnlySpan<Vector3[]> verts1, ReadOnlySpan<Vector3[]> verts2, ReadOnlySpan<Vector3[]> verts3, ReadOnlySpan<Vector2[]> uvs0, ReadOnlySpan<Vector2[]> uvs1, ReadOnlySpan<Vector2[]> uvs2, ReadOnlySpan<Vector2[]> uvs3)
    {
        _voxelCount = voxelCount;
        Assign(ref _bitfields, bitfields);
        Assign(ref _lengths0, lengths0);
        Assign(ref _lengths1, lengths1);
        Assign(ref _verts0, verts0);
        Assign(ref _verts1, verts1);
        Assign(ref _verts2, verts2);
        Assign(ref _verts3, verts3);
        Assign(ref _uvs0, uvs0);
        Assign(ref _uvs1, uvs1);
        Assign(ref _uvs2, uvs2);
        Assign(ref _uvs3, uvs3);
    }

    public int VoxelCount => _voxelCount;

    public ReadOnlySpan<ulong> Bitfields => AsSpan(ref Unsafe.AsRef(in _bitfields));

    public ReadOnlySpan<int> Lengths0 => AsSpan(ref Unsafe.AsRef(in _lengths0));
    public ReadOnlySpan<int> Lengths1 => AsSpan(ref Unsafe.AsRef(in _lengths1));

    public ReadOnlySpan<Vector3[]> Verts0 => AsSpan(ref Unsafe.AsRef(in _verts0));
    public ReadOnlySpan<Vector3[]> Verts1 => AsSpan(ref Unsafe.AsRef(in _verts1));
    public ReadOnlySpan<Vector3[]> Verts2 => AsSpan(ref Unsafe.AsRef(in _verts2));
    public ReadOnlySpan<Vector3[]> Verts3 => AsSpan(ref Unsafe.AsRef(in _verts3));

    public ReadOnlySpan<Vector2[]> UVs0 => AsSpan(ref Unsafe.AsRef(in _uvs0));
    public ReadOnlySpan<Vector2[]> UVs1 => AsSpan(ref Unsafe.AsRef(in _uvs1));
    public ReadOnlySpan<Vector2[]> UVs2 => AsSpan(ref Unsafe.AsRef(in _uvs2));
    public ReadOnlySpan<Vector2[]> UVs3 => AsSpan(ref Unsafe.AsRef(in _uvs3));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Assign<T>(ref Array6<T> field, ReadOnlySpan<T> param)
    {
        Debug.Assert(param.Length >= 6);

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ReadOnlySpan<T> AsSpan<T>(ref Array6<T> field)
#if NET8_0_OR_GREATER
        => field;
#else
        => MemoryMarshal.CreateReadOnlySpan(ref field._element0, 6);
#endif

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
