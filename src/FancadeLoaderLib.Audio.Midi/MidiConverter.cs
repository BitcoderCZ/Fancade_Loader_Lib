using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.MusicTheory;
using Serilog;
using System.Diagnostics;

namespace FancadeLoaderLib.Audio.Midi;

public sealed class MidiConverter
{
    private readonly MidiFile _file;
    private readonly MidiConvertSettings _settings;
    private readonly FcAudio.Builder _builder;

    private readonly ILogger _logger;

    private long _microsecondsPerQuarterNote = 500000;
    private readonly short _ticksPerQuarterNote;

    private readonly int _maxVelocity;

    private readonly MidiChannel[,] _channels;

    private TimeSpan[] _chunkTime;

    private MidiConverter(MidiFile file, MidiConvertSettings settings, ILogger logger)
    {
        _file = file;
        _settings = settings;
        _logger = logger;

        switch (file.TimeDivision)
        {
            case TicksPerQuarterNoteTimeDivision tpqnDivision:
                _ticksPerQuarterNote = tpqnDivision.TicksPerQuarterNote;
                _logger.Debug($"TicksPerQuarterNote: {_ticksPerQuarterNote}");
                break;
            case SmpteTimeDivision: // TODO
            default:
                throw new Exception($"Unknown TimeDivision '{file.TimeDivision.GetType()}'");
        }

        if (_ticksPerQuarterNote == 0)
        {
            throw new Exception($"TicksPerQuarterNote cannot be zero."); // divisor - can't be zero
        }

        var trackChunks = _file.Chunks.OfType<TrackChunk>();

        _chunkTime = new TimeSpan[trackChunks.Count()];

        _maxVelocity = trackChunks.Select(chunk => chunk.Events.Max(@event => @event is NoteEvent noteEvent ? (int)noteEvent.Velocity : 0)).Max();

        _builder = new FcAudio.Builder(_logger);

        _channels = new MidiChannel[trackChunks.Count(), MidiConstants.MidiChannels];
        for (int i = 0; i < _channels.GetLength(0); i++)
        {
            for (int j = 0; j < _channels.GetLength(1); j++)
            {
                _channels[i, j] = new MidiChannel();
            }
        }
    }

    public static FcAudio Convert(MidiFile file, ILogger logger, MidiConvertSettings? settings = null)
    {
        var converter = new MidiConverter(file, settings ?? MidiConvertSettings.Default, logger);

        return converter.Convert();
    }

    private FcAudio Convert()
    {
        var chunks = _file.Chunks.OfType<TrackChunk>().Select(chunk => new ChunkWithEvent(chunk)).ToArray();

        // TODO: figure out why tf I did this and if it can be removed
        for (int i = 0; i < chunks.Length; i++)
        {
            var chunk = chunks[i];
            if (chunk.Event is not null)
            {
                _chunkTime[i] += DeltaToTime(chunk.Event.DeltaTime);
                chunk.EventIndex++;
            }
        }

        while (true)
        {
            int chunkIndex = -1;
            TimeSpan lowestTime = TimeSpan.MaxValue;

            for (int i = 0; i < chunks.Length; i++)
            {
                if (chunks[i].Event is not null && _chunkTime[i] < lowestTime)
                {
                    chunkIndex = i;
                    lowestTime = _chunkTime[i];
                }
            }

            if (chunkIndex == -1)
            {
                break;
            }

            var chunk = chunks[chunkIndex];

            int processedEventCount = 0;

            var time = _chunkTime[chunkIndex];

            var firstEvent = chunk.Event;
            foreach (var @event in chunk.Events.TakeWhile(e => e == firstEvent || e.DeltaTime == 0)
#if NET7_0_OR_GREATER
                .Order(MidiEventComparer.Instance)
#else
                .OrderBy(_ => _, MidiEventComparer.Instance)
#endif
                )
            {
                _chunkTime[chunkIndex] += DeltaToTime(@event.DeltaTime);

                HandleEvent(@event, chunkIndex);

                processedEventCount++;
            }

            chunk.EventIndex += processedEventCount;
        }

        return _builder.Build();
    }

    private void HandleEvent(MidiEvent @event, int chunk)
    {
        //if (@event is ChannelEvent c && (int)c.Channel is 2 or 8)
        //{
        //	return;
        //}

        switch (@event)
        {
            case SetTempoEvent setTempo:
                HandleSetTempo(new ChunkEvent<SetTempoEvent>(setTempo, chunk));
                break;
            case NoteOnEvent noteOn:
                HandleNoteOn(new ChunkEvent<NoteOnEvent>(noteOn, chunk));
                break;
            case NoteOffEvent noteOff:
                HandleNoteOff(new ChunkEvent<NoteOffEvent>(noteOff, chunk));
                break;
            case ProgramChangeEvent programChange:
                HandleProgramChange(new ChunkEvent<ProgramChangeEvent>(programChange, chunk));
                break;
            case ControlChangeEvent controlChange:
                HandleControlChange(new ChunkEvent<ControlChangeEvent>(controlChange, chunk));
                break;
            case PitchBendEvent pitchBend:
                // TODO - new event type
                break;
            case TimeSignatureEvent:
            case SequenceTrackNameEvent:
                break; // ignore
            default:
                _logger.Warning("Unknown event type: " + @event.GetType());
                break;
        }
    }

    private void HandleSetTempo(ChunkEvent<SetTempoEvent> @event)
    {
        _microsecondsPerQuarterNote = @event.Event.MicrosecondsPerQuarterNote;
        _logger.Debug($"[{@event.Chunk}] Set tempo to {@event.Event.MicrosecondsPerQuarterNote} mc/qnote ({@event.Event.MicrosecondsPerQuarterNote / 1000f} ms/qnote)");
    }

    private void HandleProgramChange(ChunkEvent<ProgramChangeEvent> @event)
        => _channels[@event.Chunk, @event.Event.Channel].Program = @event.Event.ProgramNumber;

    private void HandleControlChange(ChunkEvent<ControlChangeEvent> @event)
    {
        MidiChannel channel = _channels[@event.Chunk, @event.Event.Channel];

        switch (@event.Event.ControlNumber)
        {
            case 6:
                // TODO
                break;
            case 7:
                channel.Volume = (double)@event.Event.ControlValue / SevenBitNumber.MaxValue;
                break;
            case 10:
                break; // ignore
            case 11:
                channel.Expression = (double)@event.Event.ControlValue / SevenBitNumber.MaxValue;
                break;
            case >= 32 and <= 64:
                // TODO
                break;
            case 91:
                // TODO
                break;
            case 92:
                // TODO
                break;
            case 93:
                // TODO
                break;
            case 94:
                // TODO
                break;
            case 95:
                // TODO
                break;
            case 96:
                // TODO
                break;
            case 97:
                // TODO
                break;
            case 98:
                // TODO
                break;
            case 99:
                // TODO
                break;
            case 100:
                // TODO
                break;
            case 101:
                // TODO
                break;
            default:
                _logger.Warning($"Unknown control number: {@event.Event.ControlNumber}");
                break;
        }
    }

    private void HandleNoteOn(ChunkEvent<NoteOnEvent> @event)
    {
        MidiChannel channel = _channels[@event.Chunk, @event.Event.Channel];
        ref int fcChannel = ref channel.MidiToFcChannel[@event.Event.NoteNumber];

        if (@event.Event.Velocity == 0)
        {
            if (fcChannel != -1)
            {
                _builder.StopSound(_chunkTime[@event.Chunk], fcChannel);
                fcChannel = -1;
            }

            return;
        }

        if (fcChannel != -1)
        {
            switch (_settings.DoubleNoteOn)
            {
                case MidiConvertSettings.DoubleNoteOnBehaviour.StopCurrent:
                    _logger.Debug($"[{@event.Event.Channel}] Tried to play sound, but one was already plaing, stopped.");
                    _builder.StopSound(_chunkTime[@event.Chunk], fcChannel);
                    fcChannel = -1;
                    break;
                case MidiConvertSettings.DoubleNoteOnBehaviour.IgnoreNew:
                    _logger.Debug($"[{@event.Event.Channel}] Tried to play sound, but one was already plaing, ignore.");
                    return;
                default:
                    Debug.Fail("Unknown settings value.");
                    goto case MidiConvertSettings.DoubleNoteOnBehaviour.StopCurrent;
            }
        }

        Note note = Note.Get(@event.Event.NoteNumber);

        _logger.Debug($"PLAY {@event.Event.NoteNumber} {_chunkTime[@event.Chunk]}");
        _builder.TryPlaySound(_chunkTime[@event.Chunk], NoteToNumb(note), ((double)@event.Event.Velocity / _maxVelocity) * channel.VolumeMult, _settings.SoundMapping[channel.Program], out fcChannel);
    }

    private void HandleNoteOff(ChunkEvent<NoteOffEvent> @event)
    {
        ref int fcChannel = ref _channels[@event.Chunk, @event.Event.Channel].MidiToFcChannel[@event.Event.NoteNumber];

        if (fcChannel == -1)
        {
            _logger.Debug($"[{@event.Event.Channel}] Tried to stop sound, but none was playing.");
        }
        else
        {
            _logger.Debug($"STOP {@event.Event.NoteNumber} {_chunkTime[@event.Chunk]}");
            _builder.StopSound(_chunkTime[@event.Chunk], fcChannel);
            fcChannel = -1;
        }
    }

    private TimeSpan DeltaToTime(long deltaTimeTicks)
#if NET7_0_OR_GREATER
        => TimeSpan.FromMicroseconds(
#else
        => new TimeSpan(10 /*TimeSpan.TicksPerMicrosecond*/ *
#endif
        (_microsecondsPerQuarterNote / _ticksPerQuarterNote) * deltaTimeTicks);

    private byte NoteToNumb(Note note)
    {
        switch (_settings.NoteOutOfRange)
        {
            case MidiConvertSettings.NoteOutOfRangeBehaviour.ClampNoteNumber:
                return note.NoteNumber; // clamped by FcAudio.PlaySound
            case MidiConvertSettings.NoteOutOfRangeBehaviour.ClampOctave:
                {
                    const int OctaveSize = 12;

                    int noteNumber = note.NoteNumber;

                    if (noteNumber < FcAudio.MinNoteNumber)
                    {
                        do
                        {
                            noteNumber += OctaveSize;
                        } while (noteNumber < FcAudio.MinNoteNumber);
                    }
                    else if (noteNumber > FcAudio.MaxNoteNumber)
                    {
                        do
                        {
                            noteNumber -= OctaveSize;
                        } while (noteNumber > FcAudio.MaxNoteNumber);
                    }

                    return (byte)noteNumber;
                }

            default:
                return note.NoteNumber;
        }
    }

    private sealed class MidiChannel
    {
        public readonly int[] MidiToFcChannel = new int[SevenBitNumber.MaxValue];

        public int Program = 0;

        public double Volume = 1d;

        public double Expression = 1d;

        public MidiChannel()
        {
            MidiToFcChannel.AsSpan().Fill(-1);
        }

        public double VolumeMult => Volume * Expression;
    }

    private sealed class ChunkWithEvent
    {
        public readonly TrackChunk Chunk;

        public ChunkWithEvent(TrackChunk chunk)
        {
            Chunk = chunk;
        }

        public int EventIndex;

        public MidiEvent? Event => EventIndex < Chunk.Events.Count ? Chunk.Events[EventIndex] : null;

        public IEnumerable<MidiEvent> Events => Chunk.Events.Skip(EventIndex);
    }

    private readonly struct ChunkEvent<T> where T : MidiEvent
    {
        public readonly T Event;
        public readonly int Chunk;

        public ChunkEvent(T @event, int chunk)
        {
            Event = @event;
            Chunk = chunk;
        }
    }

    private sealed class MidiEventComparer : IComparer<MidiEvent>
    {
        public static readonly MidiEventComparer Instance = new();

        private MidiEventComparer()
        {
        }

        public int Compare(MidiEvent? x, MidiEvent? y)
        {
            if (IsOn(x) && IsOff(y))
            {
                return 1;
            }
            else if (IsOff(x) && IsOn(y))
            {
                return -1;
            }

            return 0;
        }

        private static bool IsOn(MidiEvent? e)
            => e is NoteOnEvent on && on.Velocity > 0;

        private static bool IsOff(MidiEvent? e)
            => e is NoteOffEvent || (e is NoteOnEvent on && on.Velocity == 0);
    }
}