using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace BitcoderCZ.Fancade.Runtime;

/// <summary>
/// Represents any runtime fancade value.
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 16)]
public readonly struct RuntimeValue : IEquatable<RuntimeValue>
{
    /// <summary>
    /// A <see cref="RuntimeValue"/> with the value of 0.
    /// </summary>
    public static readonly RuntimeValue Zero = default;

    private readonly DataArray _data;

    /// <summary>
    /// Initializes a new instance of the <see cref="RuntimeValue"/> struct for a <see cref="float"/> value.
    /// </summary>
    /// <param name="value">The value to assign to the <see cref="RuntimeValue"/>.</param>
    public RuntimeValue(float value)
    {
        Write(value);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RuntimeValue"/> struct for a <see cref="Vector3"/> value.
    /// </summary>
    /// <param name="value">The value to assign to the <see cref="RuntimeValue"/>.</param>
    public RuntimeValue(Vector3 value)
    {
        Write(value);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RuntimeValue"/> struct for a <see cref="System.Numerics.Quaternion"/> value.
    /// </summary>
    /// <param name="value">The value to assign to the <see cref="RuntimeValue"/>.</param>
    public RuntimeValue(Quaternion value)
    {
        Write(value);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RuntimeValue"/> struct for a <see cref="bool"/> value.
    /// </summary>
    /// <param name="value">The value to assign to the <see cref="RuntimeValue"/>.</param>
    public RuntimeValue(bool value)
    {
        Write(value ? 1 : 0);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RuntimeValue"/> struct for an <see cref="int"/> value.
    /// </summary>
    /// <param name="value">The value to assign to the <see cref="RuntimeValue"/>.</param>
    public RuntimeValue(int value)
    {
        Write(value);
    }

    /// <summary>
    /// Gets the value of the <see cref="RuntimeValue"/> as a <see cref="float"/>.
    /// </summary>
    /// <value>Value of the <see cref="RuntimeValue"/> as a <see cref="float"/>.</value>
    public readonly float Float => Read<float>();

    /// <summary>
    /// Gets the value of the <see cref="RuntimeValue"/> as a <see cref="Vector3"/>.
    /// </summary>
    /// <value>Value of the <see cref="RuntimeValue"/> as a <see cref="Vector3"/>.</value>
    [Obsolete($"Use {nameof(Vector3)} instead.")]
    public readonly Vector3 Float3 => Read<Vector3>();

    /// <summary>
    /// Gets the value of the <see cref="RuntimeValue"/> as a <see cref="Vector3"/>.
    /// </summary>
    /// <value>Value of the <see cref="RuntimeValue"/> as a <see cref="Vector3"/>.</value>
    public readonly Vector3 Vector3 => Read<Vector3>();

    /// <summary>
    /// Gets the value of the <see cref="RuntimeValue"/> as a <see cref="Quaternion"/>.
    /// </summary>
    /// <value>Value of the <see cref="RuntimeValue"/> as a <see cref="Quaternion"/>.</value>
    public readonly Quaternion Quaternion => Read<Quaternion>();

    /// <summary>
    /// Gets the value of the <see cref="RuntimeValue"/> as a <see cref="bool"/>.
    /// </summary>
    /// <value>Value of the <see cref="RuntimeValue"/> as a <see cref="bool"/>.</value>
#pragma warning disable SA1623 // Property summary documentation should match accessors
    public readonly bool Bool => Read<int>() != 0;
#pragma warning restore SA1623 // Property summary documentation should match accessors

    /// <summary>
    /// Gets the value of the <see cref="RuntimeValue"/> as a <see cref="int"/>.
    /// </summary>
    /// <value>Value of the <see cref="RuntimeValue"/> as a <see cref="int"/>.</value>
    public readonly int Int => Read<int>();

    private readonly long Long1 => Read<long>();

    private readonly long Long2 => Read<long>(8);

    /// <summary>Returns a value that indicates whether the 2 <see cref="RuntimeValue"/>s are equal.</summary>
    /// <param name="left">The first <see cref="RuntimeValue"/> to compare.</param>
    /// <param name="right">The second <see cref="RuntimeValue"/> to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(RuntimeValue left, RuntimeValue right)
        => left.Long1 == right.Long1 && left.Long2 == right.Long2;

    /// <summary>Returns a value that indicates whether the 2 <see cref="RuntimeValue"/>s are not equal.</summary>
    /// <param name="left">The first <see cref="RuntimeValue"/> to compare.</param>
    /// <param name="right">The second <see cref="RuntimeValue"/> to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(RuntimeValue left, RuntimeValue right)
        => left.Long1 != right.Long1 || left.Long2 != right.Long2;

    /// <summary>
    /// Extracts the value of the <see cref="RuntimeValue"/> as the specified <see cref="SignalType"/>.
    /// </summary>
    /// <param name="type">The <see cref="SignalType"/>.</param>
    /// <returns>The extracted value.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="type"/> is not <see cref="SignalType.Float"/>, <see cref="SignalType.Vec3"/>, <see cref="SignalType.Rot"/> or <see cref="SignalType.Bool"/>.</exception>
    public object GetValueOfType(SignalType type)
        => type.ToNotPointer() switch
        {
            SignalType.Float => Float,
            SignalType.Vec3 => Vector3,
            SignalType.Rot => Quaternion,
            SignalType.Bool => Bool,
            _ => throw new ArgumentException($"{nameof(type)} must be {nameof(SignalType.Float)}, {nameof(SignalType.Vec3)}, {nameof(SignalType.Rot)} or {nameof(SignalType.Bool)}.", nameof(type)),
        };

    /// <inheritdoc/>
    public bool Equals(RuntimeValue other)
        => this == other;

    /// <inheritdoc/>
    public override int GetHashCode()
        => HashCode.Combine(Long1, Long2);

    /// <inheritdoc/>
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
