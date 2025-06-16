namespace FancadeLoaderLib.Audio;

public static class FcAudioConstants
{
    public const int FancadeChannels = 10;

    public static readonly TimeSpan FrameLength = TimeSpan.FromSeconds(1d / 60d);

    public const int NoteBits = 7;
    public const int NoteMaxValue = (1 << NoteBits) - 1;

    public const int VelocityBits = 6;
    public const int VelocityMaxValue = (1 << VelocityBits) - 1;

    public const int FcSoundBits = 5;
    public const int FcSoundMaxValue = (1 << FcSoundBits) - 1;

    public const int EventTypeBits = 3;
    public const int EventTypeMaxValue = (1 << EventTypeBits) - 1;

    public const int EventChannelBits = 4;
}
