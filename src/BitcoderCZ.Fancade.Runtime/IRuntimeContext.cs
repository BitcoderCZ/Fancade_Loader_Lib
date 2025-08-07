using BitcoderCZ.Fancade.Editing.Scripting.Settings;
using BitcoderCZ.Maths.Vectors;
using System.Numerics;

namespace BitcoderCZ.Fancade.Runtime;

/// <summary>
/// Methods used by <see cref="Interpreter"/> to interact with the game.
/// </summary>
public interface IRuntimeContext
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
    /// Gets the current frame of the game.
    /// </summary>
    /// <value>Current frame of the game.</value>
    long CurrentFrame { get; }

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
    void MenuItem(VariableReference? variable, FcObject picture, string name, MaxBuyCount maxBuyCount, PriceIncrease priceIncrease);

    // **************************************** Objects ****************************************

    /// <summary>
    /// Gets an object at the specified position.
    /// </summary>
    /// <remarks>
    /// Position refers to the starting position of the object, used by object wires.
    /// </remarks>
    /// <param name="position">Position of the object.</param>
    /// <param name="voxelPosition">Voxel position of the object.</param>
    /// <param name="prefabId">The prefab the object is inside of.</param>
    /// <returns>The object at the specified position.</returns>
    FcObject GetObject(int3 position, byte3 voxelPosition, ushort prefabId);

    /// <summary>
    /// Gets the position and rotation of an object.
    /// </summary>
    /// <param name="object">The object whose position and rotation should be retrieved.</param>
    /// <returns>Position and rotation of the specified object.</returns>
    (Vector3 Position, Quaternion Rotation) GetObjectPosition(FcObject @object);

    /// <summary>
    /// Sets the position and rotation of an object.
    /// </summary>
    /// <param name="object">The object whose position and/or rotation should be set.</param>
    /// <param name="position">The new position; or <see langword="null"/>, if the value should not be changed.</param>
    /// <param name="rotation">The new rotation; or <see langword="null"/>, if the value should not be changed.</param>
    void SetPosition(FcObject @object, Vector3? position, Quaternion? rotation);

    /// <summary>
    /// Performs a ray cast between 2 points.
    /// </summary>
    /// <param name="from">The start position.</param>
    /// <param name="to">The end position.</param>
    /// <returns>Information about the ray cast.</returns>
    (bool Hit, Vector3 HitPos, FcObject HitObj) Raycast(Vector3 from, Vector3 to);

    /// <summary>
    /// Gets the size of an object.
    /// </summary>
    /// <param name="object">The object whose size should be retrieved.</param>
    /// <returns>Size of the object.</returns>
    (Vector3 Min, Vector3 Max) GetSize(FcObject @object);

    /// <summary>
    /// Adds or removes an object from the world, making it visible or invisible.
    /// </summary>
    /// <param name="object">The object to add/remove.</param>
    /// <param name="visible"><see langword="true"/> if the object should be added; otherwise, <see langword="false"/>.</param>
    void SetVisible(FcObject @object, bool visible);

    /// <summary>
    /// Creates a copy of an object.
    /// </summary>
    /// <param name="original">The original object.</param>
    /// <returns>The copy.</returns>
    FcObject CreateObject(FcObject original);

    /// <summary>
    /// Destroys an object created by <see cref="CreateObject(FcObject)"/>.
    /// </summary>
    /// <param name="object">The object to destroy.</param>
    void DestroyObject(FcObject @object);

    // **************************************** Sound ****************************************

    /// <summary>
    /// Plays a sound.
    /// </summary>
    /// <param name="volume">Volume of the sound.</param>
    /// <param name="pitch">Pitch of the sound.</param>
    /// <param name="sound">The sound to play.</param>
    /// <returns>The channel the sound is playing on.</returns>
    float PlaySound(float volume, float pitch, FcSound sound);

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

    // **************************************** Physics ****************************************

    /// <summary>
    /// Adds a force to an object.
    /// </summary>
    /// <param name="object">The object to add the force to.</param>
    /// <param name="force">The linear force.</param>
    /// <param name="applyAt">The position, relative to the object, at which to apply the force.</param>
    /// <param name="torque">The angular force.</param>
    void AddForce(FcObject @object, Vector3? force, Vector3? applyAt, Vector3? torque);

    /// <summary>
    /// Get the linear and angular velocity of an object.
    /// </summary>
    /// <param name="object">The object whose velocity should be retrieved.</param>
    /// <returns><paramref name="object"/>'s linear and angular velocity.</returns>
    (Vector3 Velocity, Vector3 Spin) GetVelocity(FcObject @object);

    /// <summary>
    /// Sets the linear and angular velocity of an object.
    /// </summary>
    /// <param name="object">The object whose velocity should be set.</param>
    /// <param name="velocity">The new linear velocity; or <see langword="null"/>, if the value should not be changed.</param>
    /// <param name="spin">The new angular velocity; or <see langword="null"/>, if the value should not be changed.</param>
    void SetVelocity(FcObject @object, Vector3? velocity, Vector3? spin);

    /// <summary>
    /// Sets velocity multipliers that determine how forces affect the specified object.
    /// </summary>
    /// <param name="object">The object whose motion constraints should be set.</param>
    /// <param name="position">The new linear velocity multiplier; or <see langword="null"/>, if the value should not be changed.</param>
    /// <param name="rotation">The new angular velocity multiplier; or <see langword="null"/>, if the value should not be changed.</param>
    void SetLocked(FcObject @object, Vector3? position, Vector3? rotation);

    /// <summary>
    /// Sets the mass of an object.
    /// </summary>
    /// <param name="object">The object whose mass should be set.</param>
    /// <param name="mass">The new mass.</param>
    void SetMass(FcObject @object, float mass);

    /// <summary>
    /// Sets the friction of an object.
    /// </summary>
    /// <param name="object">The object whose friction should be set.</param>
    /// <param name="friction">The new friction.</param>
    void SetFriction(FcObject @object, float friction);

    /// <summary>
    /// Sets the bounciness of an object.
    /// </summary>
    /// <param name="object">The object whose bounciness should be set.</param>
    /// <param name="bounciness">The new bounciness.</param>
    void SetBounciness(FcObject @object, float bounciness);

    /// <summary>
    /// Sets the gravity of the game world.
    /// </summary>
    /// <param name="gravity">The new gravity, default is {0, -9.8, 0}.</param>
    void SetGravity(Vector3 gravity);

    /// <summary>
    /// Adds a constraint between 2 objects.
    /// </summary>
    /// <param name="base">Base of the constraint.</param>
    /// <param name="part">Part of the constraint (a physics-enabled object).</param>
    /// <param name="pivot">Pivot of the constraint.</param>
    /// <returns>The created constraint.</returns>
    FcConstraint AddConstraint(FcObject @base, FcObject part, Vector3? pivot);

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable SA1600 // Elements should be documented
    void LinearLimits(FcConstraint constraint, Vector3? lower, Vector3? upper);

    void AngularLimits(FcConstraint constraint, Vector3? lower, Vector3? upper);

    void LinearSpring(FcConstraint constraint, Vector3? stiffness, Vector3? damping);

    void AngularSpring(FcConstraint constraint, Vector3? stiffness, Vector3? damping);

    void LinearMotor(FcConstraint constraint, Vector3? speed, Vector3? force);

    void AngularMotor(FcConstraint constraint, Vector3? speed, Vector3? force);
#pragma warning restore SA1600 // Elements should be documented
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

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
    /// <returns><see langword="true"/> if the button was pressed last frame; otherwise, <see langword="false"/>.</returns>
    bool GetButtonPressed(ButtonType type);

    /// <summary>
    /// Creates a joystick and gets it's direction last frame.
    /// </summary>
    /// <param name="type">Type of the joystick.</param>
    /// <returns>The joystick's direction last frame.</returns>
    Vector3 GetJoystickDirection(JoystickType type);

    /// <summary>
    /// Gets if a collision happened between 2 objects.
    /// </summary>
    /// <remarks>
    /// Gets the collision with the largest <paramref name="impulse"/>.
    /// </remarks>
    /// <param name="firstObject">The objects whose collision(s) should be detected.</param>
    /// <param name="secondObject">The object that collided with <paramref name="firstObject"/>.</param>
    /// <param name="impulse">Impulse of the collision.</param>
    /// <param name="normal">Normal of the collision.</param>
    /// <returns><see langword="true"/> if <paramref name="firstObject"/> collided with another object; otherwise, <see langword="false"/>.</returns>
    bool TryGetCollision(FcObject firstObject, out FcObject secondObject, out float impulse, out Vector3 normal);

    // **************************************** Math ****************************************

    /// <summary>
    /// Sets the seed for the RNG.
    /// </summary>
    /// <param name="seed">The new seed.</param>
    void SetRandomSeed(float seed);

    /// <summary>
    /// Gets a random <see cref="float"/>.
    /// </summary>
    /// <param name="min">The minimum value.</param>
    /// <param name="max">The maximum value.</param>
    /// <returns>A random <see cref="float"/> between <paramref name="min"/> and <paramref name="max"/>.</returns>
    float GetRandomValue(float min, float max);

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
    void InspectValue(RuntimeValue value, SignalType type, string? variableName, ushort prefabId, ushort3 inspectBlockPosition);
}
