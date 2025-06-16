using System;
using System.Collections;
using System.Runtime.InteropServices;

namespace FancadeLoaderLib.Common;

public static class BitArrayUtils
{
    public static int ByteLength(this BitArray arr)
        => Maths.DivCeiling(arr.Length, 8);

    public static Span<byte> ToBytes(this BitArray arr)
    {
        // copy to is faster for int[] than byte[]
        int[] result = new int[Maths.DivCeiling(arr.Length, sizeof(int) * 8)];
        arr.CopyTo(result, 0);

        return MemoryMarshal.Cast<int, byte>(result.AsSpan())[..Maths.DivCeiling(arr.Length, 8)];
    }
}
