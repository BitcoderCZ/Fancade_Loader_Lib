using FancadeLoaderLib.Editing.Scripting;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib.Editing;

/// <summary>
/// Represents a fancade variable.
/// </summary>
public readonly struct Variable : IEquatable<Variable>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Variable"/> struct.
    /// </summary>
    /// <param name="name">Name of the variable.</param>
    /// <param name="type">Type of the variable.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is too long or <paramref name="type"/> is pointer.</exception>
    public Variable(string name, SignalType type)
    {
        ThrowIfGreaterThan(name.Length, FancadeConstants.MaxVariableNameLength);

        if (type.IsPointer())
        {
            ThrowArgumentException($"{nameof(type)} cannot be pointer.", nameof(type));
        }

        Name = name;
        Type = type;
    }

    /// <summary>
    /// Gets the name of the variable.
    /// </summary>
    /// <value>Name of the variable.</value>
    public readonly string Name { get; }

    /// <summary>
    /// Gets the type of the variable.
    /// </summary>
    /// <value>Type of the variable.</value>
    public readonly SignalType Type { get; }

    /// <summary>
    /// Gets a value indicating whether the variable is global or saved.
    /// </summary>
    /// <value><see langword="true"/> if the variable is global or saved; otherwise, <see langword="false"/>.</value>
    public bool IsGlobal => Name.StartsWith('$') || Name.StartsWith('!');

    /// <summary>Returns a value that indicates whether the 2 <see cref="Variable"/>s are equal.</summary>
    /// <param name="left">The first <see cref="Variable"/> to compare.</param>
    /// <param name="right">The second <see cref="Variable"/> to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(Variable left, Variable right)
        => left.Type == right.Type && left.Name == right.Name;

    /// <summary>Returns a value that indicates whether the 2 <see cref="Variable"/>s are not equal.</summary>
    /// <param name="left">The first <see cref="Variable"/> to compare.</param>
    /// <param name="right">The second <see cref="Variable"/> to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(Variable left, Variable right)
        => left.Type != right.Type || left.Name != right.Name;

    /// <summary>
    /// Gets the default name of a variable of a given type.
    /// </summary>
    /// <param name="type">Type of the variable.</param>
    /// <returns>Default name of a variable of the given type.</returns>
    /// <exception cref="InvalidEnumArgumentException">Thrown when <paramref name="type"/> is not a valid <see cref="SignalType"/>.</exception>
    public static string GetDefaultName(SignalType type)
        => type.ToNotPointer() switch
        {
            SignalType.Float => "Numb",
            SignalType.Vec3 => "Vec",
            SignalType.Rot => "Rot",
            SignalType.Bool => "Tru",
            SignalType.Obj => "Obj",
            SignalType.Con => "Con",
            _ => throw new InvalidEnumArgumentException(nameof(type), (int)type, typeof(SignalType)),
        };

    /// <inheritdoc/>
    public bool Equals(Variable other)
        => this == other;

    /// <inheritdoc/>
    public override string ToString()
        => $"Name: {Name}, Type: {Type}";

    /// <inheritdoc/>
    public override int GetHashCode()
        => HashCode.Combine(Name, Type);

    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is Variable other && other == this;
}
