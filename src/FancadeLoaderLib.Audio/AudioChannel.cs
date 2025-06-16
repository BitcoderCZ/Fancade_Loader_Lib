using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FancadeLoaderLib.Audio;

internal sealed class AudioChannel
{
    private readonly List<ChannelEvent> _events = [];

    public TimeSpan CurrentTime { get; private set; }

    public bool PlayingSound { get; private set; }

    public byte CurrentSound { get; private set; } = 6;

    public ReadOnlySpan<ChannelEvent> Events => CollectionsMarshal.AsSpan(_events);

    public void SetSound(TimeSpan time, byte newSound)
    {
        Debug.Assert(time >= CurrentTime);
        Debug.Assert(newSound <= FcAudioConstants.FcSoundMaxValue);

        if (newSound == CurrentSound)
        {
            return;
        }

        CurrentTime = time;
        CurrentSound = newSound;

        _events.Add(new ChannelEvent(AudioEventType.SetSound, time, CurrentSound));
    }

    public void PlaySound(TimeSpan time, byte note, byte velocity)
    {
        Debug.Assert(time >= CurrentTime);
        Debug.Assert(note <= FcAudioConstants.NoteMaxValue);
        Debug.Assert(velocity <= FcAudioConstants.VelocityMaxValue);
        Debug.Assert(!PlayingSound);

        CurrentTime = time;
        PlayingSound = true;

        _events.Add(new ChannelEvent(AudioEventType.PlaySound, time, note | ((ulong)velocity << FcAudioConstants.NoteBits)));
    }

    public void StopSound(TimeSpan time)
    {
        Debug.Assert(time >= CurrentTime);
        Debug.Assert(PlayingSound);

        CurrentTime = time;
        PlayingSound = false;

        _events.Add(new ChannelEvent(AudioEventType.StopSound, time, 0));
    }
}