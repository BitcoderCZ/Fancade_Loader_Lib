using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib.Runtime;

public readonly struct TerminalOutput
{
    public static readonly TerminalOutput Disconnected = default;

    private readonly Flags _flags;

    private readonly DataArray _data;

    public TerminalOutput(RuntimeValue value)
    {
        _flags = Flags.IsConnected;

        Write(value);
    }

    public TerminalOutput(VariableReference reference)
    {
        _flags = Flags.IsReference | Flags.IsConnected;

        Write(reference);
    }

    [Flags]
    private enum Flags : byte
    {
        IsReference = 1 << 0,
        IsConnected = 1 << 1,
    }

    public readonly bool IsReference => (_flags & Flags.IsReference) == Flags.IsReference;

    public readonly bool IsConnected => (_flags & Flags.IsConnected) == Flags.IsConnected;

    public readonly RuntimeValue Value
    {
        get
        {
            if (IsReference)
            {
                ThrowInvalidOperationException("Cannot get the value of a reference, use GetValue instead.");
            }

            return Read<RuntimeValue>();
        }
    }

    public readonly VariableReference Reference
    {
        get
        {
            if (!IsReference)
            {
                ThrowInvalidOperationException("Cannot get the reference of a value.");
            }

            return Read<VariableReference>();
        }
    }

    public readonly RuntimeValue GetValue(IRuntimeContext context)
        => IsReference ? Reference.GetValue(context) : Value;

#pragma warning disable SA1114
    private void Write<T>(T value)
        => Unsafe.WriteUnaligned(
#if NET8_0_OR_GREATER
            ref MemoryMarshal.GetReference((ReadOnlySpan<byte>)_data),
#else
            ref Unsafe.AsRef(in _data._element0),
#endif
            value);

    private T Read<T>()
        => Unsafe.ReadUnaligned<T>(
#if NET8_0_OR_GREATER
            ref MemoryMarshal.GetReference((ReadOnlySpan<byte>)_data));
#else
            ref Unsafe.AsRef(in _data._element0));
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
