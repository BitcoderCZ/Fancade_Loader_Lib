using System.Collections.Frozen;
using System.Numerics;

namespace BitcoderCZ.Fancade;

/// <summary>
/// Represents the type of a wire/terminal.
/// </summary>
public enum SignalType
{
    /// <summary>
    /// Invalid type.
    /// </summary>
    Error = 0,

    /// <summary>
    /// Void type (execution wire).
    /// </summary>
    Void = 1,

    /// <summary>
    /// Float type.
    /// </summary>
    Float = 2,

    /// <summary>
    /// Float type pointer (variable).
    /// </summary>
    FloatPtr = 3,

    /// <summary>
    /// Vector type.
    /// </summary>
    Vec3 = 4,

    /// <summary>
    /// Vector type pointer (variable).
    /// </summary>
    Vec3Ptr = 5,

    /// <summary>
    /// Rotation type.
    /// </summary>
    Rot = 6,

    /// <summary>
    /// Rotation type pointer (variable).
    /// </summary>
    RotPtr = 7,

    /// <summary>
    /// Truth type.
    /// </summary>
    Bool = 8,

    /// <summary>
    /// Truth type pointer (variable).
    /// </summary>
    BoolPtr = 9,

    /// <summary>
    /// Object type.
    /// </summary>
    Obj = 10,

    /// <summary>
    /// Object type pointer (variable).
    /// </summary>
    ObjPtr = 11,

    /// <summary>
    /// Constraint type.
    /// </summary>
    Con = 12,

    /// <summary>
    /// Constraint type pointer (variable).
    /// </summary>
    ConPtr = 13,
}

/// <summary>
/// Utils for <see cref="SignalType"/>.
/// </summary>
#pragma warning disable SA1649 // File name should match first type name - it fucking does???
public static class SignalTypeUtils
#pragma warning restore SA1649
{
    private static readonly FrozenDictionary<Type, SignalType> TypeToSignalType = new Dictionary<Type, SignalType>()
    {
        [typeof(float)] = SignalType.Float,
        [typeof(Vector3)] = SignalType.Vec3,
        [typeof(Rotation)] = SignalType.Rot,
        [typeof(bool)] = SignalType.Bool,
    }.ToFrozenDictionary();

    /// <summary>
    /// Gets the corresponding <see cref="SignalType"/> for a <see cref="Type"/>.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to get the <see cref="SignalType"/> for.</param>
    /// <returns>The <see cref="SignalType"/> corresponding to <paramref name="type"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="type"/> doesn't correspond to any <see cref="SignalType"/>.</exception>
    public static SignalType FromType(Type type)
        => TypeToSignalType.TryGetValue(type, out var signalType)
            ? signalType
            : throw new ArgumentException($"Type '{type?.FullName ?? "null"}' doesn't map to any setting type.", nameof(type));

    /// <summary>
    /// Converts a <see cref="SignalType"/> to the pointer version (<see cref="SignalType.Float"/> to <see cref="SignalType.FloatPtr"/>).
    /// </summary>
    /// <param name="signalType">The <see cref="SignalType"/> to convert.</param>
    /// <returns>The converted <see cref="SignalType"/>.</returns>
    public static SignalType ToPointer(this SignalType signalType)
        => signalType == SignalType.Error ? signalType : (SignalType)((int)signalType | 1);

    /// <summary>
    /// Converts a <see cref="SignalType"/> to the non pointer version (<see cref="SignalType.FloatPtr"/> to <see cref="SignalType.Float"/>).
    /// </summary>
    /// <param name="signalType">The <see cref="SignalType"/> to convert.</param>
    /// <returns>The converted <see cref="SignalType"/>.</returns>
    public static SignalType ToNotPointer(this SignalType signalType)
        => signalType == SignalType.Void ? signalType : (SignalType)((int)signalType & (int.MaxValue ^ 1));

    /// <summary>
    /// Determines if a <see cref="SignalType"/> is a pointer.
    /// </summary>
    /// <param name="signalType">The <see cref="SignalType"/> to check.</param>
    /// <returns><see langword="true"/> if <paramref name="signalType"/> is a pointer; otherwise, <see langword="false"/>.</returns>
    public static bool IsPointer(this SignalType signalType)
        => ((int)signalType & 1) == 1;
}