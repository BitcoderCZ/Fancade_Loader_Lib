// <copyright file="IFcInputHandler.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Fancade.Editing.Scripting.Settings;
using BitcoderCZ.Fancade.Runtime.Utils;
using System.Numerics;
using static BitcoderCZ.Fancade.Utils.ThrowHelper;

namespace BitcoderCZ.Fancade.Runtime.Input;

/// <summary>
/// Provides a way of getting touches and swipes.
/// </summary>
public interface IFcInputHandler
{
    /// <summary>
    /// Maximum amount of buttons active at a time.
    /// </summary>
    static readonly int MaxButtonCount = 4;

    /// <summary>
    /// Maxmimum amount of detectable touches.
    /// </summary>
    static readonly int MaxTouchCount = 3;

#pragma warning disable SA1600 // Elements should be documented
    internal const float ButtonBoundsLandscape = 0.12f;
    internal const float ButtonBoundsPortrait = 0.25f;
#pragma warning restore SA1600 // Elements should be documented

    /// <summary>
    /// Specifies the texture of a <see cref="Button"/>.
    /// </summary>
#pragma warning disable SA1400 // Access modifier should be declared
    enum ButtonTexture
#pragma warning restore SA1400 // Access modifier should be declared
    {
        /// <summary>
        /// The button is up arrow.
        /// </summary>
        UpArrow,

        /// <summary>
        /// The button is down arrow.
        /// </summary>
        DownArrow,

        /// <summary>
        /// The button is left arrow.
        /// </summary>
        LeftArrow,

        /// <summary>
        /// The button is right arrow.
        /// </summary>
        RightArrow,

        /// <summary>
        /// The button is circle with Z in it.
        /// </summary>
        CircleZ,

        /// <summary>
        /// The button is circle with X in it.
        /// </summary>
        CircleX,

        /// <summary>
        /// The button is circle with C in it.
        /// </summary>
        CircleC,

        /// <summary>
        /// The button is circle with V in it.
        /// </summary>
        CircleV,
    }

    /// <summary>
    /// Gets the amount of active buttons of the type <see cref="ButtonType.Direction"/>.
    /// </summary>
    /// <value>Amount of active buttons of the type <see cref="ButtonType.Direction"/>.</value>
    int DirectionButtonCount { get; }

    /// <summary>
    /// Gets the amount of active buttons of the type <see cref="ButtonType.Button"/>.
    /// </summary>
    /// <value>Amount of active buttons of the type <see cref="ButtonType.Button"/>.</value>
    int NormalButtonCount { get; }

    /// <summary>
    /// Gets the position of a touch.
    /// </summary>
    /// <param name="state">State of the touch to detect.</param>
    /// <param name="fingerIndex">The finger whose touch should be detected.</param>
    /// <param name="touchPos">Position of the touch.</param>
    /// <returns><see langword="true"/> if the specified touch is active; otherwise, <see langword="false"/>.</returns>
    bool TryGetTouch(TouchState state, int fingerIndex, out Vector2 touchPos);

    /// <summary>
    /// Gets the direction of a swipe.
    /// </summary>
    /// <param name="direction">Direction of the swipe.</param>
    /// <returns><see langword="true"/> if a swipe is active; otherwise, <see langword="false"/>.</returns>
    bool TryGetSwipe(out Vector3 direction);

    /// <summary>
    /// Gets the active buttons.
    /// </summary>
    /// <returns>The active buttons.</returns>
    IEnumerable<Button> GetActiveButtons();

    /// <summary>
    /// Creates a button and gets if it was pressed last frame.
    /// </summary>
    /// <param name="type">Type of the button.</param>
    /// <param name="blockPosition">Environment and position of the block currently being executed.</param>
    /// <returns><see langword="true"/> if the button was pressed last frame; otherwise, <see langword="false"/>.</returns>
    bool GetButtonPressed(ButtonType type, EnvironmentPosition blockPosition);

    /// <summary>
    /// Gets the positions of the touches.
    /// </summary>
    /// <param name="positions">A buffer to write the positions into, must be at least <see cref="MaxTouchCount"/> in length.</param>
    virtual void GetTouchPositions(Span<Vector2?> positions)
    {
        ThrowIfLessThan(positions.Length, MaxTouchCount);

        for (int i = 0; i < 3; i++)
        {
            if (TryGetTouch(TouchState.Touching, i, out var pos))
            {
                positions[i] = pos;
            }
            else
            {
                positions[i] = null;
            }
        }
    }

    /// <summary>
    /// Calculates the normalized horizontal bounds (minimum and maximum values between 0 and 1) for a button.
    /// </summary>
    /// <param name="buttonIndex">The zero-based index of the button within its own type (direction or normal).</param>
    /// <param name="buttonType">The type of button.</param>
    /// <param name="directionButtonCount">The total number of direction buttons (<see cref="ButtonType.Direction"/>).</param>
    /// <param name="normalButtonCount">The total number of normal buttons (<see cref="ButtonType.Button"/>).</param>
    /// <param name="screenInfo">A <see cref="ScreenInfo"/> instance representing the current viewport.</param>
    /// <returns>A tuple containing the minimum and maximum normalized horizontal bounds for the button.</returns>
    static (float Min, float Max) GetButtonBounds(int buttonIndex, ButtonType buttonType, int directionButtonCount, int normalButtonCount, ScreenInfo screenInfo)
    {
        float sideButtonBounds = screenInfo.IsLandscape ? ButtonBoundsLandscape : ButtonBoundsPortrait;

        int totalButtonCount = directionButtonCount + normalButtonCount;

        ThrowIfGreaterThanOrEqualToOrNegative(buttonIndex, buttonType is ButtonType.Direction ? directionButtonCount : normalButtonCount);
        ThrowIfGreaterThanOrEqualToOrNegative(totalButtonCount, MaxButtonCount);

        // direction buttons are always on the left
        int totalButtonIndex = buttonType is ButtonType.Direction ? buttonIndex : buttonIndex + directionButtonCount;

        // todo: this could be optimized
        return totalButtonCount switch
        {
            1 => (0f, 1f),
            2 => totalButtonIndex is 0 ? (0f, 0.5f) : (0.5f, 1f),
            3 => directionButtonCount >= 2
                ? totalButtonIndex switch
                {
                    0 => (0f, sideButtonBounds),
                    1 => (sideButtonBounds, 0.5f),
                    2 => (0.5f, 1f),
                    _ => default,
                } // 2 on left
                : totalButtonIndex switch
                {
                    0 => (0f, 0.5f),
                    1 => (0.5f, 1f - sideButtonBounds),
                    2 => (1f - sideButtonBounds, 1f),
                    _ => default,
                }, // 2 on right
            4 => totalButtonIndex switch
            {
                0 => (0f, sideButtonBounds),
                1 => (sideButtonBounds, 0.5f),
                2 => (0.5f, 1f - sideButtonBounds),
                3 => (1f - sideButtonBounds, 1f),
                _ => default,
            },
            _ => default,
        };
    }

    /// <summary>
    /// Represents an active button.
    /// </summary>
#pragma warning disable SA1400 // Access modifier should be declared
    readonly struct Button
#pragma warning restore SA1400 // Access modifier should be declared
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Button"/> struct.
        /// </summary>
        /// <param name="pressed"><see langword="true"/> if the button is pressed; otherwise, <see langword="false"/>.</param>
        /// <param name="type">Type of the button.</param>
        /// <param name="texture">Texture of the button.</param>
        /// <param name="horizontalPosition">Horizontal positions of the button, between 0 and 1.</param>
        public Button(bool pressed, ButtonType type, ButtonTexture texture, float horizontalPosition)
        {
            ThrowIfNotInRangeInclusive(horizontalPosition, 0f, 1f);

            Pressed = pressed;
            Type = type;
            Texture = texture;
            HorizontalPosition = horizontalPosition;
        }

        /// <summary>
        /// Gets a value indicating whether the button is pressed.
        /// </summary>
        /// <value><see langword="true"/> if the button is pressed; otherwise, <see langword="false"/>.</value>
        public bool Pressed { get; }

        /// <summary>
        /// Gets the type of the button.
        /// </summary>
        /// <value>Type of the button.</value>
        public ButtonType Type { get; }

        /// <summary>
        /// Gets the texture of the button.
        /// </summary>
        /// <value>Texture of the button.</value>
        public ButtonTexture Texture { get; }

        /// <summary>
        /// Gets the horizontal positions of the button.
        /// </summary>
        /// <value>Horizontal positions of the button, between 0 and 1.</value>
        public float HorizontalPosition { get; }
    }
}