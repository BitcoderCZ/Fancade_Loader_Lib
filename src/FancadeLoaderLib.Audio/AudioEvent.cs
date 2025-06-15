using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace FancadeLoaderLib.Audio;

public readonly struct AudioEvent : IComparable<AudioEvent>
{
    public const int MaxSizeInBytes = sizeof(AudioEventType) + sizeof(byte) + sizeof(int) + sizeof(ulong);

    public readonly AudioEventType Type;
    public readonly byte Channel;
    public readonly int Frame;
    public readonly ulong Data;

    public AudioEvent(AudioEventType type, byte channel, int frame, ulong data)
    {
        Debug.Assert(frame >= 0);

        Type = type;
        Channel = channel;
        Frame = frame;
        Data = data;
    }

    public int CompareTo(AudioEvent other)
    {
        int comp = Frame.CompareTo(other.Frame);
        if (comp != 0)
        {
            return comp;
        }

        comp = Channel.CompareTo(other.Channel);
        if (comp != 0)
        {
            return comp;
        }

        comp = ((byte)Type).CompareTo((byte)other.Type);
        return comp;
    }

    public override string ToString()
        => $"{Type}, Channel: {Channel}, Frame: {Frame}";
}