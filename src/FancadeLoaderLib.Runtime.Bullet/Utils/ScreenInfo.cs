using System.Numerics;

namespace FancadeLoaderLib.Runtime.Bullet.Utils;

internal readonly struct ScreenInfo
{
    public float Width { get; }
    public float Height { get; }
    public float AspectRatio { get; }
    public bool Portrait => (int)Width <= (int)Height;
    public bool Landscape => (int)Height < (int)Width;

    public ScreenInfo(Vector2 screenSize)
        : this(screenSize.X, screenSize.Y)
    {
    }

    public ScreenInfo(float width, float height)
    {
        Width = width;
        Height = height;
        AspectRatio = Height / Width;
    }
}
