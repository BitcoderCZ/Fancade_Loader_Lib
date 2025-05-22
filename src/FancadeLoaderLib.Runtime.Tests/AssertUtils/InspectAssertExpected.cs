using MathUtils.Vectors;
using System.Diagnostics;

namespace FancadeLoaderLib.Runtime.Tests.AssertUtils;

// TODO: allow specifying if assert should (not) be in late update
internal readonly struct InspectAssertExpected : ISpanFormattable
{
    public InspectAssertExpected(object value)
        : this(value, value switch
        {
            float => SignalType.Float,
            float3 => SignalType.Vec3,
            Rotation => SignalType.Rot,
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

    public readonly bool? BoxArt { get; init; }

    public readonly InspectFrequency? Frequency { get; init; }

    public readonly int? Count { get; init; }

    public readonly int? FrameCount { get; init; }

    public readonly int? Order { get; init; }

    public override string ToString()
        => $"{this}";

    public string ToString(string? format, IFormatProvider? provider)
        => ToString();

    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        => destination.TryWrite(provider, $"{{'{Value}'{(Position is { } position ? $", Position: {position}" : "")}{(Frequency is { } frequency ? $", Frequency: {frequency}" : "")}{(Count is { } count ? $", Count: {count}" : "")}{(FrameCount is { } frameCount ? $", FrameCount: {frameCount}" : "")}{(Order is { } order ? $", Order: {order}" : "")}{(BoxArt is { } boxArt ? $", BoxArt: {boxArt}" : "")}}}", out charsWritten);
}
