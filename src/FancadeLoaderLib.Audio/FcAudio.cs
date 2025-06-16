using FancadeLoaderLib.Common;
using Serilog;
using System.Collections;
using System.Diagnostics;
using System.Numerics;
using static FancadeLoaderLib.Utils.ThrowHelper;

using FcSound = FancadeLoaderLib.Editing.Scripting.Settings.FcSound;

namespace FancadeLoaderLib.Audio;

public sealed class FcAudio
{
    public readonly List<AudioEvent> Events;

    public FcAudio()
    {
        Events = [];
    }

    public FcAudio(int capacity)
    {
        Events = new(capacity);
    }

    public FcAudio(IEnumerable<AudioEvent> events)
    {
        Events = [.. events];
    }

    internal FcAudio(ReadOnlySpan<AudioChannel> channels)
    {
        Debug.Assert(channels.Length <= FcAudioConstants.FancadeChannels);

        int eventCount = 0;
        foreach (var channel in channels)
        {
            eventCount += channel.Events.Length;
        }

        Events = new List<AudioEvent>(eventCount);

        for (byte i = 0; i < channels.Length; i++)
        {
            var channel = channels[i];

            foreach (var @event in channel.Events)
            {
                Events.Add(new AudioEvent(@event.Type, i, checked((int)(@event.Time / FcAudioConstants.FrameLength)), @event.Data));
            }
        }
    }

    public Span<byte> ToData(out int eventCount, ILogger? logger = null)
    {
        Events.Sort();

        DataBuilder builder = new DataBuilder(Events.Count, logger);

        eventCount = Events.Count;
        for (int i = 0; i < Events.Count; i++)
        {
            eventCount += builder.WriteEvent(Events[i]);
        }

        return builder.Build();
    }

    public sealed class Builder
    {
        private readonly ILogger _logger;

        private readonly AudioChannel[] _channels = new AudioChannel[FcAudioConstants.FancadeChannels];

        public Builder(ILogger logger)
        {
            _logger = logger;

            for (int i = 0; i < _channels.Length; i++)
            {
                _channels[i] = new AudioChannel();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="time"></param>
        /// <param name="note">The note to play, will be clamped to 48-84.</param>
        /// <param name="velocity"></param>
        /// <param name="sound"></param>
        /// <param name="fcChannel"></param>
        /// <returns></returns>
        public bool TryPlaySound(TimeSpan time, byte note, double velocity, FcSound sound, out int fcChannel)
        {
            byte soundByte = (byte)sound;

            Debug.Assert(time.Ticks >= 0);
            Debug.Assert(velocity is >= 0d and <= 1d);
            Debug.Assert(note <= FcAudioConstants.NoteMaxValue);
            Debug.Assert(soundByte <= FcAudioConstants.FcSoundMaxValue);

            if (!TryFindUnusedChannel(time, soundByte, out fcChannel))
            {
                _logger.Debug("Tried to play sound, but all channels were already in use.");
                return false;
            }

            // notes are in range 0-127, but fancade can only *really* play notes from 48, and it clamps pitch at 4, so the max value is 84
            note = Math.Clamp(note, (byte)48, (byte)84);

            // TODO: make this configurable
            velocity = Math.Pow(velocity, 1d);

            //Log.Debug($"PLAY [{fcChannel}] {time}");
            _channels[fcChannel].PlaySound(time, note, (byte)(velocity * FcAudioConstants.VelocityMaxValue));
            return true;
        }

        public void StopSound(TimeSpan time, int fcChannel)
        {
            ThrowIfNegative(fcChannel);
            ThrowIfGreaterThanOrEqual(fcChannel, _channels.Length);

            Debug.Assert(time.Ticks >= 0);

            if (!_channels[fcChannel].PlayingSound)
            {
                _logger.Debug("Tried to stop sound, but none was playing.");
                return;
            }
            else if (_channels[fcChannel].CurrentTime > time)
            {
                _logger.Debug("Tried to stop sound, but channel time was greater than the stop time.");
                return;
            }

            //Log.Debug($"STOP [{fcChannel}] {time}");
            _channels[fcChannel].StopSound(time);
        }

        public FcAudio Build()
            => new FcAudio(_channels);

        private bool TryFindUnusedChannel(TimeSpan time, byte sound, out int channelIndex)
        {
            // TODO: ideally dela in frames, if a sound is started and stopped on the same frame, it's sound channel becomes awailable only on the next frame

            // try to find a channel with the current sound
            for (int i = 0; i < _channels.Length; i++)
            {
                if (!_channels[i].PlayingSound && time >= _channels[i].CurrentTime && _channels[i].CurrentSound == sound)
                {
                    channelIndex = i;
                    return true;
                }
            }

            for (int i = 0; i < _channels.Length; i++)
            {
                if (!_channels[i].PlayingSound && time >= _channels[i].CurrentTime)
                {
                    _channels[i].SetSound(time, sound);

                    channelIndex = i;
                    return true;
                }
            }

            channelIndex = -1;
            return false;
        }
    }

    private sealed class DataBuilder
    {
        private readonly BitArray _data;
        private int _index;
        private int _lastFrame;

        private readonly ILogger? _logger;

        public DataBuilder(ILogger? logger)
        {
            _data = new BitArray(0);
            _logger = logger;
        }

        public DataBuilder(int initialCapacity, ILogger? logger)
        {
            _data = new BitArray(initialCapacity * AudioEvent.MaxSizeInBytes * 8);
            _logger = logger;
        }

        /// <returns>The number of additional events written (wait for example).</returns>
        public int WriteEvent(AudioEvent @event)
        {
            Debug.Assert(@event.Frame >= _lastFrame);

            int delta = @event.Frame - _lastFrame;

            int waitCount = 0;
            if (delta > 0)
            {
                waitCount = WriteWait(delta);
            }

            _lastFrame = @event.Frame;

            switch (@event.Type)
            {
                case AudioEventType.PlaySound:
                    WritePlaySound(@event.Channel, @event.Data);
                    break;
                case AudioEventType.StopSound:
                    WriteStopSound(@event.Channel);
                    break;
                case AudioEventType.SetSound:
                    WriteSetSound(@event.Channel, @event.Data);
                    break;
                default:
                    Debug.Fail($"Unknown or invalid event type: {@event.Type}");
                    break;
            }

            return waitCount;
        }

        public Span<byte> Build()
        {
            _data.Length = _index;

            return _data.ToBytes();
        }

        private void WritePlaySound(byte channel, ulong data)
        {
            Debug.Assert(channel < FcAudioConstants.FancadeChannels);

            Debug.Assert((byte)AudioEventType.PlaySound <= FcAudioConstants.EventTypeMaxValue);

            _logger?.Debug($"Play sound {(data >> 4) & 0b1111111} on channel {channel}");

            WriteBits((byte)AudioEventType.PlaySound, FcAudioConstants.EventTypeBits);

            WriteBits(channel, FcAudioConstants.EventChannelBits);

            WriteBits(data, FcAudioConstants.NoteBits + FcAudioConstants.VelocityBits);
        }

        private void WriteStopSound(byte channel)
        {
            Debug.Assert(channel < FcAudioConstants.FancadeChannels);

            Debug.Assert((byte)AudioEventType.StopSound <= FcAudioConstants.EventTypeMaxValue);

            _logger?.Debug($"Stop sound on channel {channel}");

            WriteBits((byte)AudioEventType.StopSound, FcAudioConstants.EventTypeBits);

            WriteBits(channel, FcAudioConstants.EventChannelBits);
        }

        private void WriteSetSound(byte channel, ulong data)
        {
            Debug.Assert(channel < FcAudioConstants.FancadeChannels);

            Debug.Assert((byte)AudioEventType.SetSound <= FcAudioConstants.EventTypeMaxValue);

            _logger?.Debug($"Set sound on channel {channel} to {data}");

            WriteBits((byte)AudioEventType.SetSound, FcAudioConstants.EventTypeBits);

            WriteBits(channel, FcAudioConstants.EventChannelBits);

            WriteBits(data, FcAudioConstants.FcSoundBits);
        }

        private int WriteWait(int frames)
        {
            int waitCount = 0;

            while (frames > 15)
            {
                // wait with delay 0 is actually a wait of 1 frame, because wait 0 doesn't make sense
                int min = Math.Min(256, frames);

                WriteLongWait((byte)(min - 1));
                waitCount++;

                frames -= min;
            }

            while (frames > 0)
            {
                // wait with delay 0 is actually a wait of 1 frame, because wait 0 doesn't make sense
                int min = Math.Min(16, frames);

                WriteShortWait((byte)(min - 1));
                waitCount++;

                frames -= min;
            }

            return waitCount;
        }

        private void WriteShortWait(byte frames)
        {
            Debug.Assert(frames < 16);
            Debug.Assert((byte)AudioEventType.ShortWait <= FcAudioConstants.EventTypeMaxValue);

            _logger?.Debug($"Wait {frames + 1} frames");

            WriteBits((byte)AudioEventType.ShortWait, FcAudioConstants.EventTypeBits);
            WriteBits(frames, 4);
        }

        private void WriteLongWait(byte frames)
        {
            Debug.Assert((byte)AudioEventType.LongWait <= FcAudioConstants.EventTypeMaxValue);

            _logger?.Debug($"Wait {frames + 1} frames");

            WriteBits((byte)AudioEventType.LongWait, FcAudioConstants.EventTypeBits);
            WriteBits(frames, 8);
        }

#if NET7_0_OR_GREATER
        private void WriteBits<T>(T value, int numbBits) where T : IBinaryInteger<T>
        {
            Debug.Assert(numbBits <= int.CreateChecked(T.PopCount(T.AllBitsSet)));

            if (_index + numbBits > _data.Length)
            {
                _data.Length += AudioEvent.MaxSizeInBytes * 8 * 256;
            }

            for (int i = 0; i < numbBits; i++)
            {
                _data[_index++] = ((value >> i) & T.One) == T.One;
            }
        }
#else
        private void WriteBits(byte value, int numbBits)
        {
            Debug.Assert(numbBits <= sizeof(byte) * 8);

            if (_index + numbBits > _data.Length)
            {
                _data.Length += AudioEvent.MaxSizeInBytes * 8 * 256;
            }

            for (int i = 0; i < numbBits; i++)
            {
                _data[_index++] = ((value >> i) & 1) == 1;
            }
        }

        private void WriteBits(ulong value, int numbBits)
        {
            Debug.Assert(numbBits <= sizeof(ulong) * 8);

            if (_index + numbBits > _data.Length)
            {
                _data.Length += AudioEvent.MaxSizeInBytes * 8 * 256;
            }

            for (int i = 0; i < numbBits; i++)
            {
                _data[_index++] = ((value >> i) & 1) == 1;
            }
        }
#endif
    }
}