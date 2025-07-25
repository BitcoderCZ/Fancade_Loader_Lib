using Melanchall.DryWetMidi.Common;

using FcSound = FancadeLoaderLib.Editing.Scripting.Settings.FcSound;

namespace FancadeLoaderLib.Audio.Midi;

public sealed class MidiConvertSettings
{
    public static readonly MidiConvertSettings Default = new MidiConvertSettings();

    /// <summary>
    /// Maps midi programs to fancade sounds.
    /// </summary>
    public readonly FcSound[] SoundMapping = new FcSound[SevenBitNumber.MaxValue + 1];

    /// <summary>
    /// The behaviour for when the same note is played twice without stopping the first one.
    /// </summary>
    public DoubleNoteOnBehaviour DoubleNoteOn { get; set; } = DoubleNoteOnBehaviour.StopCurrent;

    /// <summary>
    /// The behaviour for when the note number is out of the range supported by Fancade (currently 48-84).
    /// </summary>
    public NoteOutOfRangeBehaviour NoteOutOfRange { get; set; } = NoteOutOfRangeBehaviour.ClampOctave;

    public MidiConvertSettings()
    {
        var mappingSpan = SoundMapping.AsSpan();

        mappingSpan.Fill(FcSound.Piano);

        // TODO: this could be better
        mappingSpan[0..8].Fill(FcSound.Piano); // Piano Timbres
        mappingSpan[8..16].Fill(FcSound.Marimba); // Chromatic Percussion
        mappingSpan[14] = FcSound.Clang; // Tubular Bells
        mappingSpan[16..24].Fill(FcSound.Pad); // ORGAN
        mappingSpan[38..40].Fill(FcSound.Pad); // Synth Bass 1,2
        mappingSpan[48..56].Fill(FcSound.Pad); // ENSEMBLE
        mappingSpan[56..64].Fill(FcSound.Pad); // BRASS
        mappingSpan[64..72].Fill(FcSound.Pad); // REED
        mappingSpan[80..88].Fill(FcSound.Pad); // Synth Lead
        mappingSpan[88..96].Fill(FcSound.Pad); // Synth Pad
        mappingSpan[123] = FcSound.Chirp; // Bird Tweet
        mappingSpan[127] = FcSound.Boom; // Gun Shot
    }

    public enum DoubleNoteOnBehaviour
    {
        /// <summary>
        /// Stops the currently playing note and plays the new one.
        /// </summary>
        StopCurrent = 0,
        /// <summary>
        /// Ignores the new note, the current one continues playing.
        /// </summary>
        IgnoreNew,
    }

    public enum NoteOutOfRangeBehaviour
    {
        /// <summary>
        /// Clamps the note number to the valid range.
        /// </summary>
        ClampNoteNumber = 0,
        /// <summary>
        /// Clamps the note's octave value to the valid range.
        /// </summary>
        ClampOctave,
    }
}