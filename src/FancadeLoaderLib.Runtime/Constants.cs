using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("FancadeLoaderLib.Runtime.Tests")]

namespace FancadeLoaderLib.Runtime;

internal static class Constants
{
    public const float EqualsNumbersMaxDiff = 0.001f;
    public const float EqualsVectorsMaxDiff = 1.0000001e-06f;
}
