// <copyright file="IRuntimeContextBase.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Fancade.Editing.Scripting.Settings;
using BitcoderCZ.Fancade.Runtime.Utils;
using BitcoderCZ.Maths.Vectors;
using System.Numerics;

namespace BitcoderCZ.Fancade.Runtime;

/// <summary>
/// Base methods used by <see cref="IAstRunner"/>s to interact with the game.
/// </summary>
/// <remarks>
/// Implementations of this interface must ensure that none of its methods throw exceptions.  
/// Consumers of this interface are not required to handle exceptions.
/// </remarks>
public interface IRuntimeContextBase
{
    /// <summary>
    /// Gets the size of the screen in pixels.
    /// </summary>
    /// <value>Size of the screen in pixels.</value>
    Vector2 ScreenSize { get; }

    /// <summary>
    /// Gets the value of the accelerometer sensor.
    /// </summary>
    /// <remarks>
    /// If not available, returns {0, -9.8, 0}.
    /// </remarks>
    /// <value>Value of the accelerometer sensor.</value>
    Vector3 Accelerometer { get; }

    /// <summary>
    /// Gets a value indicating whether the game is in the box art mode.
    /// </summary>
    /// <value><see langword="true"/> if the game is in the box art mode; otherwise, <see langword="false"/>.</value>
    bool TakingBoxArt { get; }

    // **************************************** Game ****************************************

    /// <summary>
    /// Wins the game after a specified delay.
    /// </summary>
    /// <param name="delay">The delay to win, in frames.</param>
    void Win(int delay);

    /// <summary>
    /// Loses the game after a specified delay.
    /// </summary>
    /// <param name="delay">The delay to loss, in frames.</param>
    void Lose(int delay);

    /// <summary>
    /// Sets the score, coins, and ranking.
    /// </summary>
    /// <param name="score">The new score; or <see langword="null"/>, if the value should not be changed.</param>
    /// <param name="coins">The new coin count; or <see langword="null"/>, if the value should not be changed.</param>
    /// <param name="ranking">The new ranking mode.</param>
    void SetScore(float? score, float? coins, Ranking ranking);

    /// <summary>
    /// Sets the camera position, rotation, range, and perspective mode.
    /// </summary>
    /// <param name="position">The new position; or <see langword="null"/>, if the value should not be changed.</param>
    /// <param name="rotation">The new rotation; or <see langword="null"/>, if the value should not be changed.</param>
    /// <param name="range">The new range; or <see langword="null"/>, if the value should not be changed.</param>
    /// <param name="perspective">If <see langword="true"/>, the camera will be in perspective mode; otherwise, the camera will be in orthographic mode.</param>
    void SetCamera(Vector3? position, Quaternion? rotation, float? range, bool perspective);

    /// <summary>
    /// Sets the direction of light.
    /// </summary>
    /// <param name="position">The new position; or <see langword="null"/>, if the value should not be changed.</param>
    /// <param name="rotation">The new rotation; or <see langword="null"/>, if the value should not be changed.</param>
    void SetLight(Vector3? position, Quaternion? rotation);

    /// <summary>
    /// Registers a menu item to the shop, or creates a shop section.
    /// </summary>
    /// <param name="variable">The variable to store the purchase count into; or null, if a store section should be created.</param>
    /// <param name="picture">The object to use for the item icon.</param>
    /// <param name="name">Name of the item or section.</param>
    /// <param name="maxBuyCount">The maximum number of times the item can be bought.</param>
    /// <param name="priceIncrease">Determines how the price of the item increases.</param>
    void MenuItem(Variable? variable, FcObject picture, string name, MaxBuyCount maxBuyCount, PriceIncrease priceIncrease);

    // **************************************** Sound ****************************************

    /// <summary>
    /// Plays a sound.
    /// </summary>
    /// <param name="volume">Volume of the sound.</param>
    /// <param name="pitch">Pitch of the sound.</param>
    /// <param name="loop"><see langword="true"/> if the sound should loop; otherwise, <see langword="false"/>.</param>
    /// <param name="sound">The sound to play.</param>
    /// <returns>The channel the sound is playing on.</returns>
    float PlaySound(float volume, float pitch, bool loop, FcSound sound);

    /// <summary>
    /// Stops a sound.
    /// </summary>
    /// <param name="channel">The channel the sound is playing on.</param>
    void StopSound(float channel);

    /// <summary>
    /// Changes the volume and pitch of a sound.
    /// </summary>
    /// <param name="channel">The channel the sound is playing on.</param>
    /// <param name="volume">The new volume; or <see langword="null"/>, if the value should not be changed.</param>
    /// <param name="pitch">The new pitch; or <see langword="null"/>, if the value should not be changed.</param>
    void AdjustVolumePitch(float channel, float? volume, float? pitch);

    // **************************************** Control ****************************************

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
    /// Creates a button and gets if it was pressed last frame.
    /// </summary>
    /// <param name="type">Type of the button.</param>
    /// <param name="blockPosition">Environment and position of the block currently being executed.</param>
    /// <returns><see langword="true"/> if the button was pressed last frame; otherwise, <see langword="false"/>.</returns>
    bool GetButtonPressed(ButtonType type, EnvironmentPosition blockPosition);

    /// <summary>
    /// Creates a joystick and gets it's direction last frame.
    /// </summary>
    /// <param name="type">Type of the joystick.</param>
    /// <param name="blockPosition">Environment and position of the block currently being executed.</param>
    /// <returns>The joystick's direction last frame.</returns>
    Vector3 GetJoystickDirection(JoystickType type, EnvironmentPosition blockPosition);

    // **************************************** Math ****************************************

    /// <summary>
    /// Converts a screen coordinates to world coordinates.
    /// </summary>
    /// <param name="screenPos">The screen coordinates.</param>
    /// <returns>The position in world; 2 and 400 units away from the camera, respectively.</returns>
    (Vector3 WorldNear, Vector3 WorldFar) ScreenToWorld(Vector2 screenPos);

    /// <summary>
    /// Converts a world position to screen coordinates.
    /// </summary>
    /// <param name="worldPos">The world position to convert.</param>
    /// <returns>Screen coordinates at which the world position appears.</returns>
    Vector2 WorldToScreen(Vector3 worldPos);

    // **************************************** Values ****************************************

    /// <summary>
    /// Inspects a value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="type">Type of <paramref name="value"/>.</param>
    /// <param name="variableName">Name of the inspected variable, if one was inspected; otherwise, <see langword="null"/>.</param>
    /// <param name="prefabId">Id of the prefab the inspect block is located in.</param>
    /// <param name="inspectBlockPosition">Position of the inspect block.</param>
    void InspectValue(RuntimeValue value, SignalType type, string? variableName, ushort prefabId, int3 inspectBlockPosition);
}