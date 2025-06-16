namespace FancadeLoaderLib.Audio;

public enum AudioEventType : byte
{
    /// <summary>
    /// Internal event, not valid for <see cref="ChannelEvent"/> or <see cref="AudioEvent"/>.
    /// </summary>
    /// <remarks>
    /// 8bits - the wait time in frames -1 (0 val = wait 1 frame).
    /// </remarks>
    LongWait = 0,
    /// <summary>
    /// Internal event, not valid for <see cref="ChannelEvent"/> or <see cref="AudioEvent"/>.
    /// </summary>
    /// <remarks>
    /// 4bits - the wait time in frames -1 (0 val = wait 1 frame).
    /// </remarks>
    ShortWait = 1,
    /// <summary>
    /// Stops the currently playing sound on a channel.
    /// </summary>
    /// <remarks>
    /// <see cref="FcAudioConstants.EventChannelBits"/> - the channel to stop the sound on.
    /// </remarks>
    StopSound = 2,
    /// <summary>
    /// Sets the sound to play on a channel.
    /// </summary>
    /// <remarks>
    /// <see cref="FcAudioConstants.EventChannelBits"/> - the channel to set the sound of.
    /// <see cref="FcAudioConstants.FcSoundBits"/> - the sound the play on the channel.
    /// </remarks>
    SetSound = 3,
    /// <summary>
    /// Starts playing a sound.
    /// </summary>
    /// <remarks>
    /// <see cref="FcAudioConstants.EventChannelBits"/> - the channel to stop the sound on.
    /// <see cref="FcAudioConstants.NoteBits"/> - the note to play.
    /// <para><see cref="FcAudioConstants.VelocityBits"/> - the velocity/volume of the sound.</para>
    /// </remarks>
    PlaySound = 4,
}
