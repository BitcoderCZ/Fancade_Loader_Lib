using System.Numerics;

namespace BitcoderCZ.Fancade.Runtime.Utils;

/// <summary>
/// A struct to store the size and aspect ratio of the screen.
/// </summary>
public readonly struct ScreenInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScreenInfo"/> struct.
    /// </summary>
    /// <param name="screenSize">Size of the screen in pixels.</param>
    public ScreenInfo(Vector2 screenSize)
        : this(screenSize.X, screenSize.Y)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScreenInfo"/> struct.
    /// </summary>
    /// <param name="width">Width of the screen in pixels.</param>
    /// <param name="height">Height of the screen in pixels.</param>
    public ScreenInfo(float width, float height)
    {
        Width = width;
        Height = height;
        AspectRatio = Height / Width;
    }

    /// <summary>
    /// Gets the width of the screen.
    /// </summary>
    /// <value>Width of the screen.</value>
    public readonly float Width { get; }

    /// <summary>
    /// Gets the height of the screen.
    /// </summary>
    /// <value>Height of the screen.</value>
    public readonly float Height { get; }

    /// <summary>
    /// Gets the aspect ratio of the screen.
    /// </summary>
    /// <value>Aspect ratio of the screen.</value>
    public readonly float AspectRatio { get; }

    /// <summary>
    /// Gets a value indicating whether the screen is in portrait orientation.
    /// </summary>
    /// <value><see langword="true"/> if the screen is in portrait orientation; otherwise, <see langword="false"/>.</value>
    public readonly bool Portrait => (int)Width <= (int)Height;

    /// <summary>
    /// Gets a value indicating whether the screen is in landscape orientation.
    /// </summary>
    /// <value><see langword="true"/> if the screen is in landscape orientation; otherwise, <see langword="false"/>.</value>
    public readonly bool Landscape => (int)Height < (int)Width;
}