using BitcoderCZ.Fancade.Raw;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BitcoderCZ.Fancade;

/// <summary>
/// Represents a "setting" of a prefab.
/// <para>For example: if setCamera is perspective, the sound of playSound, ...</para>
/// </summary>
public readonly struct PrefabSetting : IEquatable<PrefabSetting>
{
    private readonly SettingType _type;
    private readonly object _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="PrefabSetting"/> struct.
    /// </summary>
    /// <param name="type">Type of this setting.</param>
    /// <param name="value">Value of this setting.</param>
    public PrefabSetting(SettingType type, object value)
    {
        if (!RawPrefabSetting.IsValueValid(value, type))
        {
            throw new ArgumentException($"Type of value '{value?.GetType()?.FullName ?? "null"}' isn't valid for type '{type}'", nameof(value));
        }

        _type = type;
        _value = value;
    }

    /// <summary>
    /// Gets the type of this setting.
    /// </summary>
    /// <value>Type of this setting.</value>
    public readonly SettingType Type => _type;

    /// <summary>
    /// Gets the value of this setting.
    /// </summary>
    /// <value>Value of this setting.</value>
    public readonly object Value => _value;

    /// <summary>Returns a value that indicates whether the 2 <see cref="PrefabSetting"/>s are equal.</summary>
    /// <param name="left">The first <see cref="PrefabSetting"/> to compare.</param>
    /// <param name="right">The second <see cref="PrefabSetting"/> to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(PrefabSetting left, PrefabSetting right)
        => left.Type == right.Type && Equals(left.Value, right.Value);

    /// <summary>Returns a value that indicates whether the 2 <see cref="PrefabSetting"/>s are not equal.</summary>
    /// <param name="left">The first <see cref="PrefabSetting"/> to compare.</param>
    /// <param name="right">The second <see cref="PrefabSetting"/> to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(PrefabSetting left, PrefabSetting right)
        => left.Type != right.Type || !Equals(left.Value, right.Value);

    /// <summary>
    /// Gets the value as <typeparamref name="T"/>. If <see cref="Value"/> is a <see cref="string"/> returns <see langword="default"/>.
    /// </summary>
    /// <remarks>
    /// If <typeparamref name="T"/> is not the corresponding <see cref="SettingType"/> of <see cref="Type"/>, <see cref="Value"/> is bitcasted to <typeparamref name="T"/>.
    /// </remarks>
    /// <typeparam name="T">The type to get the value as.</typeparam>
    /// <returns><see cref="Value"/> as <typeparamref name="T"/>.</returns>
    public T GetValue<T>()
        where T : unmanaged
    {
        if (Value is string)
        {
            return default;
        }

        if (typeof(T) == RawPrefabSetting.GetTypeForSettingType(Type))
        {
            return (T)Value;
        }

        // largest value type is 12 bytes (Vector3)
        Span<byte> buffer = stackalloc byte[12];

#pragma warning disable CS9191 // The 'ref' modifier for an argument corresponding to 'in' parameter is equivalent to 'in'. Consider using 'in' instead.
        switch (Value)
        {
            case byte b:
                buffer[0] = b;
                break;
            case ushort us:
                MemoryMarshal.Write(buffer, ref us);
                break;
            case int i:
                MemoryMarshal.Write(buffer, ref i);
                break;
            case float f:
                MemoryMarshal.Write(buffer, ref f);
                break;
            case System.Numerics.Vector3 v:
                MemoryMarshal.Write(buffer, ref v);
                break;
            default:
                Debug.Fail($"{nameof(Value)} has an invalid type: {Value.GetType()}.");
                break;
        }
#pragma warning restore CS9191 // The 'ref' modifier for an argument corresponding to 'in' parameter is equivalent to 'in'. Consider using 'in' instead.

        return MemoryMarshal.Read<T>(buffer);
    }

    /// <summary>
    /// Returns the string representation of the current instance.
    /// </summary>
    /// <returns>The string representation of the current instance.</returns>
    public readonly override string ToString()
        => $"Type: {Type}, Value: {Value}";

    /// <inheritdoc/>
    public readonly bool Equals(PrefabSetting other)
        => this == other;

    /// <inheritdoc/>
    public readonly override bool Equals(object? obj)
        => obj is PrefabSetting other && this == other;

    /// <inheritdoc/>
    public readonly override int GetHashCode()
        => HashCode.Combine(Type, Value);
}