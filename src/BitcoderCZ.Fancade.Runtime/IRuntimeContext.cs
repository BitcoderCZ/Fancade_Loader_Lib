using BitcoderCZ.Maths.Vectors;
using System.Numerics;

namespace BitcoderCZ.Fancade.Runtime;

/// <summary>
/// Methods used by <see cref="IAstRunner"/>s to interact with the game.
/// </summary>
public interface IRuntimeContext : IRuntimeContextBase
{
    /// <summary>
    /// Gets the current frame of the game.
    /// </summary>
    /// <value>Current frame of the game.</value>
    long CurrentFrame { get; }

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
    /// <param name="environment">The environment of hte object.</param>
    /// <param name="blockPosition">Position of the <see cref="Syntax.Objects.GetPositionExpressionSyntax"/> node.</param>
    /// <returns>Position and rotation of the specified object.</returns>
    (Vector3 Position, Quaternion Rotation) GetObjectPosition(FcObject @object, IFcEnvironment environment, int3 blockPosition);

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
    void LinearLimits(FcConstraint constraint, Vector3 lower, Vector3 upper);

    void AngularLimits(FcConstraint constraint, Vector3 lower, Vector3 upper);

    void LinearSpring(FcConstraint constraint, Vector3 stiffness, Vector3 damping);

    void AngularSpring(FcConstraint constraint, Vector3 stiffness, Vector3 damping);

    void LinearMotor(FcConstraint constraint, Vector3 speed, Vector3 force);

    void AngularMotor(FcConstraint constraint, Vector3 speed, Vector3 force);
#pragma warning restore SA1600 // Elements should be documented
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

    // **************************************** Control ****************************************

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
}
