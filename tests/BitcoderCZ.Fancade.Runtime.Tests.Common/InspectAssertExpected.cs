using BitcoderCZ.Maths.Vectors;
using System.Diagnostics;
using System.Numerics;

namespace BitcoderCZ.Fancade.Runtime.Tests.Common;

// TODO: allow specifying if assert should (not) be in late update
public readonly struct InspectAssertExpected : ISpanFormattable, IEquatable<InspectAssertExpected>
{
    public InspectAssertExpected(object value)
        : this(value, value switch
        {
            float => SignalType.Float,
            Vector3 => SignalType.Vec3,
            Rotation => SignalType.Rot,
            Quaternion => SignalType.Rot,
            bool => SignalType.Bool,
            FcObject => SignalType.Obj,
            FcConstraint => SignalType.Con,
            _ => throw new UnreachableException(),
        })
    {
    }

    public InspectAssertExpected(object value, SignalType type)
    {
        Value = value;
        Type = type;
    }

    public readonly object Value { get; }

    public readonly SignalType Type { get; }

    public readonly int3? Position { get; init; }

    [Obsolete]
    public readonly bool? BoxArt { get; init; }

    public readonly InspectFrequency? Frequency { get; init; }

    public readonly int? Count { get; init; }

    public readonly int? FrameCount { get; init; }

    public readonly int? Order { get; init; }

    public override bool Equals(object? obj) 
        => obj is InspectAssertExpected expected && Equals(expected);

    public bool Equals(InspectAssertExpected other)
        => EqualityComparer<object>.Default.Equals(Value, other.Value) && Type == other.Type && Position == other.Position && Frequency == other.Frequency && Count == other.Count && FrameCount == other.FrameCount && Order == other.Order;

    public override int GetHashCode() 
        => HashCode.Combine(Value, Type, Position, Frequency, Count, FrameCount, Order);

    public override string ToString()
        => $"{this}";

    public string ToString(string? format, IFormatProvider? provider)
        => ToString();

    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        => destination.TryWrite(provider, $"{{'{Value}'{(Position is { } position ? $", Position: {position}" : "")}{(Frequency is { } frequency ? $", Frequency: {frequency}" : "")}{(Count is { } count ? $", Count: {count}" : "")}{(FrameCount is { } frameCount ? $", FrameCount: {frameCount}" : "")}{(Order is { } order ? $", Order: {order}" : "")}{(BoxArt is { } boxArt ? $", BoxArt: {boxArt}" : "")}}}", out charsWritten);

    public static bool operator ==(InspectAssertExpected left, InspectAssertExpected right) => left.Equals(right);
    public static bool operator !=(InspectAssertExpected left, InspectAssertExpected right) => !(left == right);
}
