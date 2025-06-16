using System;
using System.Diagnostics;

namespace FancadeLoaderLib.Common;

public static class BitUtils
{
    public static void Copy8To6Bit(ReadOnlySpan<byte> src, Span<byte> dest)
    {
        Debug.Assert(dest.Length >= Maths.DivCeiling(src.Length * 8, 6));

        int destIndex = 0;
        for (int i = 0; i < src.Length; i += 3)
        {
            if (i + 3 > src.Length)
            {
                if (i + 2 > src.Length)
                {
                    dest[destIndex++] = (byte)(src[i] & 0b_111111);
                    dest[destIndex++] = (byte)(src[i] >> 6);
                }
                else
                {
                    dest[destIndex++] = (byte)(src[i] & 0b_111111);
                    dest[destIndex++] = (byte)((src[i] >> 6) | ((src[i + 1] & 0b_1111) << 2));
                    dest[destIndex++] = (byte)(src[i + 1] >> 4);
                }
            }
            else
            {
                dest[destIndex++] = (byte)(src[i] & 0b_111111);
                dest[destIndex++] = (byte)((src[i] >> 6) | ((src[i + 1] & 0b_1111) << 2));
                dest[destIndex++] = (byte)((src[i + 1] >> 4) | ((src[i + 2] & 0b_11) << 4));
                dest[destIndex++] = (byte)(src[i + 2] >> 2);
            }
        }
    }
}