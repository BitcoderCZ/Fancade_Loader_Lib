using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace BitcoderCZ.Fancade.Runtime;

/// <summary>
/// Represents the output of a terminal, <see cref="RuntimeValue"/> or <see cref="VariableReference"/>.
/// </summary>
public readonly struct TerminalOutput
{
    /// <summary>
    /// Represents a null terminal.
    /// </summary>
    public static readonly TerminalOutput Disconnected = default;

    private readonly Flags _flags;

    private readonly DataArray _data = default;

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalOutput"/> struct for <see cref="RuntimeValue"/>.
    /// </summary>
    /// <param name="value">The value to assign to the <see cref="TerminalOutput"/>.</param>
    public TerminalOutput(RuntimeValue value)
    {
        _flags = Flags.IsConnected;

        Write(value);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalOutput"/> struct for <see cref="VariableReference"/>.
    /// </summary>
    /// <param name="reference">The value to assign to the <see cref="TerminalOutput"/>.</param>
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

    /// <summary>
    /// Gets a value indicating whether the <see cref="TerminalOutput"/> is a reference to a variable.
    /// </summary>
    /// <value>If <see langword="true"/>, the <see cref="TerminalOutput"/> contains a <see cref="VariableReference"/>.
    /// <para>If <see langword="false"/>, the <see cref="TerminalOutput"/> contains a <see cref="RuntimeValue"/>.</para></value>
    public readonly bool IsReference => (_flags & Flags.IsReference) == Flags.IsReference;

    /// <summary>
    /// Gets a value indicating whether the <see cref="TerminalOutput"/> is connected.
    /// </summary>
    /// <value><see langword="true"/> if the <see cref="TerminalOutput"/> is connected; otherwise, <see langword="false"/>.</value>
    public readonly bool IsConnected => (_flags & Flags.IsConnected) == Flags.IsConnected;

    /// <summary>
    /// Gets the <see cref="RuntimeValue"/> stored in the <see cref="TerminalOutput"/>.
    /// Should only be called when <see cref="IsReference"/> is <see langword="false"/>. 
    /// </summary>
    /// <value>The <see cref="RuntimeValue"/> stored in the <see cref="TerminalOutput"/>.</value>
    public readonly RuntimeValue Value => Read<RuntimeValue>();

    /// <summary>
    /// Gets the <see cref="VariableReference"/> stored in the <see cref="TerminalOutput"/>.
    /// Should only be called when <see cref="IsReference"/> is <see langword="true"/>. 
    /// </summary>
    /// <value>The <see cref="VariableReference"/> stored in the <see cref="TerminalOutput"/>.</value>
    public readonly VariableReference Reference => Read<VariableReference>();

    /// <summary>
    /// Gets the <see cref="RuntimeValue"/>, regardless of <see cref="IsReference"/>.
    /// </summary>
    /// <param name="variableAccessor">The <see cref="IVariableAccessor"/> used to resolve the value when <see cref="IsReference"/> is <see langword="true"/>.</param>
    /// <returns>The stored <see cref="RuntimeValue"/>, or the resolved value from the <see cref="VariableReference"/>.</returns>
    public readonly RuntimeValue GetValue(IVariableAccessor variableAccessor)
        => IsReference ? Reference.GetValue(variableAccessor) : Value;

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
