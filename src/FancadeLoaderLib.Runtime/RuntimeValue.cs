using MathUtils.Vectors;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FancadeLoaderLib.Runtime;

[StructLayout(LayoutKind.Sequential, Size = 16)]
public readonly struct RuntimeValue : IEquatable<RuntimeValue>
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

    public RuntimeValue(Quaternion value)
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

    public readonly Quaternion Quaternion => Read<Quaternion>();

    public readonly bool Bool => Read<int>() != 0;

    public readonly int Int => Read<int>();

    private readonly long Long1 => Read<long>();

    private readonly long Long2 => Read<long>(8);

    public static bool operator ==(RuntimeValue left, RuntimeValue right)
        => left.Long1 == right.Long1 && left.Long2 == right.Long2;

    public static bool operator !=(RuntimeValue left, RuntimeValue right)
        => left.Long1 != right.Long1 || left.Long2 != right.Long2;

    public object GetValueOfType(SignalType type)
        => type.ToNotPointer() switch
        {
            SignalType.Float => Float,
            SignalType.Vec3 => Float3,
            SignalType.Rot => Quaternion,
            SignalType.Bool => Bool,
            _ => throw new ArgumentException($"{nameof(type)} must be {nameof(SignalType.Float)}, {nameof(SignalType.Vec3)}, {nameof(SignalType.Rot)} or {nameof(SignalType.Bool)}.", nameof(type)),
        };

    public bool Equals(RuntimeValue other)
        => this == other;

    public override int GetHashCode()
        => HashCode.Combine(Long1, Long2);

    public override bool Equals(object? obj)
        => obj is RuntimeValue other && this == other;

#pragma warning disable SA1114
    private void Write<T>(T value)
        => Unsafe.WriteUnaligned(
#if NET8_0_OR_GREATER
            ref MemoryMarshal.GetReference((ReadOnlySpan<byte>)_data),
#else
            ref Unsafe.AsRef(in _data._element0),
#endif
            value);

    private readonly T Read<T>(nuint offset = 0)
        => Unsafe.ReadUnaligned<T>(
#if NET8_0_OR_GREATER
            ref Unsafe.AddByteOffset(ref MemoryMarshal.GetReference((ReadOnlySpan<byte>)_data), offset));
#else
            ref Unsafe.AddByteOffset(ref Unsafe.AsRef(in _data._element0), offset));
#endif
#pragma warning restore SA1114

#if NET8_0_OR_GREATER
    [InlineArray(16)]
    private struct DataArray
    {
        private byte _element0;
    }
#else

    [StructLayout(LayoutKind.Sequential, Size = 16)]
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
        private byte _element12;
        private byte _element13;
        private byte _element14;
        private byte _element15;
#pragma warning restore IDE0044 // Add readonly modifier
    }
#endif
}
