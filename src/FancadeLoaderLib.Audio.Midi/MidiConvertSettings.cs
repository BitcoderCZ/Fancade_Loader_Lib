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

    public MidiConvertSettings()
    {
        var mappingSpan = SoundMapping.AsSpan();

        mappingSpan.Fill(FcSound.Piano);

        // TODO: this could be better
        mappingSpan[0..8].Fill(FcSound.Piano); // Piano Timbres
        mappingSpan[8..16].Fill(FcSound.Clang); // Chromatic Percussion
        mappingSpan[49..56].Fill(FcSound.Pad); // ENSEMBLE
        mappingSpan[56..63].Fill(FcSound.Pad); // BRASS
        mappingSpan[64..71].Fill(FcSound.Pad); // REED
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
}