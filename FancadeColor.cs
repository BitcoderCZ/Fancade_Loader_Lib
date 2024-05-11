using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FancadeLoaderLib
{
    public enum FancadeColor : byte
    {
        LightBrown = 9,
        Brown = 8,
        DarkBrown = 7,
        LightTan = 12,
        Tan = 11,
        DarkTan = 10,
        LightPurple = 30,
        Purple = 29,
        DarkPurple = 28,
        LightPink = 33,
        Pink = 32,
        DarkPink = 31,
        LightRed = 15,
        Red = 14,
        DarkRed = 13,
        LightOrange = 18,
        Orange = 17,
        DarkOrange = 16,
        LightYellow = 21,
        Yellow = 20,
        DarkYellow = 19,
        LightGreen = 24,
        Green = 23,
        DarkGreen = 22,
        LightBlue = 27,
        Blue = 26,
        DarkBlue = 25,
        White = 6,
        Gray1 = 5,
        Gray2 = 4,
        Gray3 = 3,
        Gray4 = 2,
        Black = 1
    }

    public static class FancadeColorE
    {
        public const FancadeColor Default = FancadeColor.Blue;
    }
}
