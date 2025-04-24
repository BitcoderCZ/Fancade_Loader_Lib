using System.Diagnostics.CodeAnalysis;
using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib.Runtime;

public readonly struct Variable : IEquatable<Variable>
{
    public Variable(string name, SignalType type)
    {
        if (type.IsPointer())
        {
            ThrowArgumentException($"{nameof(type)} cannot be pointer.", nameof(type));
        }

        Name = name;
        Type = type;
    }

    public readonly string Name { get; }

    public readonly SignalType Type { get; }

    public bool IsGlobal => Name.StartsWith('$') || Name.StartsWith('!');

    public static bool operator ==(Variable left, Variable right)
        => left.Type == right.Type && left.Name == right.Name;

    public static bool operator !=(Variable left, Variable right)
        => left.Type != right.Type || left.Name != right.Name;

    public static string GetDefaultName(SignalType type)
        => type.ToNotPointer() switch
        {
            SignalType.Float => "Numb",
            SignalType.Vec3 => "Vec",
            SignalType.Rot => "Rot",
            SignalType.Bool => "Tru",
            SignalType.Obj => "Obj",
            SignalType.Con => "Con",
            _ => throw new ArgumentException($"{nameof(type)} must be a valid variable type.", nameof(type)),
        };

    public bool Equals(Variable other)
        => this == other;

    public override string ToString()
        => $"Name: {Name}, Type: {Type}";

    public override int GetHashCode()
        => HashCode.Combine(Name, Type);

    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is Variable other && other == this;
}
