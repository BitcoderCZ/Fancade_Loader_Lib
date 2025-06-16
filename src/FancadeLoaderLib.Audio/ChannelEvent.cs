using System.Diagnostics;

namespace FancadeLoaderLib.Audio;

internal readonly struct ChannelEvent
{
    public readonly AudioEventType Type;
    public readonly TimeSpan Time;
    public readonly ulong Data;

    public ChannelEvent(AudioEventType type, TimeSpan time, ulong data)
    {
        Debug.Assert(time.Ticks >= 0);

        Type = type;
        Time = time;
        Data = data;
    }

    public override string ToString()
        => $"{Type}, Time: {Time}";
}
