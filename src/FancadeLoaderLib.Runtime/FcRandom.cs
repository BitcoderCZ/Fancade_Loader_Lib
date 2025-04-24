using System.Diagnostics;

namespace FancadeLoaderLib.Runtime;

public sealed class FcRandom
{
    private ulong _state;

    public FcRandom()
    {
        DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime now = DateTime.UtcNow;
        _state = (ulong)(int)(now - epoch).TotalSeconds;
    }

    public void SetSeed(float seed)
        => _state = (ulong)seed; // not 100% sure this is how fancade does this, but testes it with a few values and seemed to work

    public float NextSingle(float min = 0f, float max = 1f)
    {
        const float RAND_NORMALIZER = 1.0f / 32768.0f;

        _state = (_state * 0x41c64e6d) + 0x3039;

        float randomFactor = ((uint)_state >> 16 & 0x7fff) * RAND_NORMALIZER;

        Debug.Assert(randomFactor >= 0f && randomFactor <= 1f, $"{nameof(randomFactor)} should be between 0 and 1.");

        return min + ((max - min) * randomFactor);
    }
}
