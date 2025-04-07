using MathUtils.Vectors;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FancadeLoaderLib.Runtime;

[StructLayout(LayoutKind.Sequential, Size = 12)]
public readonly struct RuntimeValue
{
    public static readonly RuntimeValue Zero = default;

    private readonly DataArray _data;

    public RuntimeValue(float value)
    {
        Write(value);
    }

    public RuntimeValue(float3 value)
    {
        Write(value);
    }

    public RuntimeValue(bool value)
    {
        Write(value);
    }

    public RuntimeValue(int value)
    {
        Write(value);
    }

    public readonly float Float => Read<float>();

    public readonly float3 Float3 => Read<float3>();

    public readonly bool Bool => Read<int>() != 0;

    public readonly int Int => Read<int>();

    private void Write<T>(T value)
        => Unsafe.WriteUnaligned(
#if NET8_0_OR_GREATER
            ref MemoryMarshal.GetReference((ReadOnlySpan<byte>)_data),
#else
            ref Unsafe.AsRef(in _data._element0),
#endif
            value);

    private readonly T Read<T>()
        => Unsafe.ReadUnaligned<T>(
#if NET8_0_OR_GREATER
            ref MemoryMarshal.GetReference((ReadOnlySpan<byte>)_data)
#else
            ref Unsafe.AsRef(in _data._element0)
#endif
            );

#if NET8_0_OR_GREATER
    [InlineArray(12)]
    private struct DataArray
    {
        private byte _element0;
    }
#else

    [StructLayout(LayoutKind.Sequential, Size = 12)]
    private struct DataArray
    {
#pragma warning disable IDE0044 // Add readonly modifier
        public byte _element0;
        private byte _element1;
        private byte _element2;
        private byte _element3;
        private byte _element4;
        private byte _element5;
        private byte _element6;
        private byte _element7;
        private byte _element8;
        private byte _element9;
        private byte _element10;
        private byte _element11;
#pragma warning restore IDE0044 // Add readonly modifier
    }
#endif
}
