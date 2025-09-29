using BitcoderCZ.BulletSharp;
using System.Diagnostics;
using System.Numerics;
using static BitcoderCZ.Fancade.Utils.ThrowHelper;

namespace BitcoderCZ.Fancade.Runtime.Simulated.Bullet;

/// <summary>
/// Represents a runtime fancade object.
/// </summary>
public sealed class RuntimeObject : IDisposable
{
    private float _mass = 1f;

    internal RuntimeObject(FcObject id, ushort outsidePrefabId, short inPrefabMeshIndex, RigidBody rigidBody, Vector3 pos, Quaternion rot, Vector3 sizeMin, Vector3 sizeMax, float mass, bool visible, bool @fixed)
    {
        if (id == FcObject.Null)
        {
            ThrowArgumentException($"{nameof(id)} cannot be equal to {nameof(FcObject)}.{nameof(FcObject.Null)}.", nameof(id));
        }

        ThrowIfNegative(inPrefabMeshIndex);

        Id = id;
        OutsidePrefabId = outsidePrefabId;
        InPrefabMeshIndex = inPrefabMeshIndex;
        RigidBody = rigidBody;
        Pos = pos;
        Rot = rot;
        Start = new StartValues(pos, mass, visible, @fixed);
        SizeMin = sizeMin;
        SizeMax = sizeMax;
        _mass = mass;
        IsVisible = visible;
    }

    private RuntimeObject(FcObject id, ushort outsidePrefabId, short inPrefabMeshIndex, RigidBody rigidBody, Vector3 pos, Quaternion rot, StartValues start, Vector3 sizeMin, Vector3 sizeMax, float mass, bool isVisible, bool isUserCreated)
    {
        Id = id;
        OutsidePrefabId = outsidePrefabId;
        InPrefabMeshIndex = inPrefabMeshIndex;
        RigidBody = rigidBody;
        Pos = pos;
        Rot = rot;
        Start = start;
        SizeMin = sizeMin;
        SizeMax = sizeMax;
        _mass = mass;
        IsVisible = isVisible;
        IsUserCreated = isUserCreated;
    }

    /// <summary>
    /// Gets the id of the object.
    /// </summary>
    /// <value>Id of the object.</value>
    public FcObject Id { get; }

    /// <summary>
    /// Gets the id of the prefab the object is in.
    /// </summary>
    /// <value>Id of the prefab the object is in.</value>
    public ushort OutsidePrefabId { get; }

    /// <summary>
    /// Gets the id of the mesh of the object.
    /// </summary>
    /// <value>Id of the mesh of the object.</value>
    public short InPrefabMeshIndex { get; }

    /// <summary>
    /// Gets the object's <see cref="BulletSharp.RigidBody"/>.
    /// </summary>
    /// <value>Object's <see cref="BulletSharp.RigidBody"/>.</value>
    public RigidBody RigidBody { get; }

    /// <summary>
    /// Gets the object's position.
    /// </summary>
    /// <value>Object's position.</value>
    public Vector3 Pos { get; private set; }

    /// <summary>
    /// Gets the object's rotation.
    /// </summary>
    /// <value>Object's rotation.</value>
    public Quaternion Rot { get; private set; }

    /// <summary>
    /// Gets the object's size min.
    /// </summary>
    /// <value>Object's size min.</value>
    public Vector3 SizeMin { get; }

    /// <summary>
    /// Gets the object's size max.
    /// </summary>
    /// <value>Object's size max.</value>
    public Vector3 SizeMax { get; }

    /// <summary>
    /// Gets or sets the object's mass.
    /// </summary>
    /// <value>Object's mass.</value>
    public float Mass
    {
        get => _mass;
        set
        {
            RigidBody.SetMassProps(value, RigidBody.CollisionShape.CalculateLocalInertia(value));
            RigidBody.UpdateInertiaTensor();
            if (RigidBody.IsInWorld)
            {
                RigidBody.Activate(true);
            }

            _mass = value;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the object is user created.
    /// </summary>
    /// <value><see langword="true"/> if the object is user created (by a create object block); otherwise, <see langword="false"/>.</value>
    public bool IsUserCreated { get; init; } = false;

    /// <summary>
    /// Gets a value indicating whether the object is visible.
    /// </summary>
    /// <value><see langword="true"/> if the object is visible; otherwise, <see langword="false"/>.</value>
    public bool IsVisible { get; internal set; } = true;

    /// <summary>
    /// Gets a value indicating whether the object is fixed (it's physics are disabled).
    /// </summary>
    /// <value><see langword="true"/> if the object is fixed; otherwise, <see langword="false"/>.</value>
    public bool IsFixed { get; private set; } = true;

    /// <summary>
    /// Gets the current frames most forceful collision.
    /// </summary>
    /// <value>Current frames most forceful collision.</value>
    public CollisionInfo MaxForceCollision { get; internal set; } = CollisionInfo.Default;

    internal StartValues Start { get; }

    /// <summary>
    /// Sets the position and/or rotation of the object.
    /// </summary>
    /// <param name="position">The new position; or <see langword="null"/>, if the position should not be changed.</param>
    /// <param name="rotation">The new rotation; or <see langword="null"/>, if the position should not be changed.</param>
    public void SetRotPos(Vector3? position, Quaternion? rotation)
    {
        Debug.Assert(RigidBody.MotionState is not null, "MotionState should not be null.");
        var mat = RigidBody.MotionState.WorldTransform;

        if (position is { } pos)
        {
            Pos = pos;
            mat.Translation = pos;
        }

        if (rotation is { } rot)
        {
            Rot = rot;
            mat.SetRotation(rot, out mat);
        }

        if (position is not null || rotation is not null)
        {
            RigidBody.WorldTransform = mat;
            RigidBody.MotionState.WorldTransform = mat;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        RigidBody.MotionState?.Dispose();
        RigidBody.Dispose();
    }

    internal RuntimeObject Clone(FcObject newId, RigidBody newBody, bool userCreated)
    {
        var newObject = new RuntimeObject(newId, OutsidePrefabId, InPrefabMeshIndex, newBody, Pos + Vector3.One, Rot, Start, SizeMin, SizeMax, Mass, true, userCreated);

        return newObject;
    }

    internal void Update()
    {
        Debug.Assert(RigidBody.MotionState is not null, "MotionState should not be null.");

        // for some reason in some situations not updated
        //var wt = RigidBody.MotionState.WorldTransform;
        //Debug.Assert(wt == RigidBody.WorldTransform);
        var wt = RigidBody.WorldTransform;

        Pos = wt.Translation;
        Rot = wt.GetRotation();
    }

    internal void Reset(DynamicsWorld world, IRuntimeContext ctx)
    {
        RigidBody.Friction = 0.5f;
        RigidBody.Restitution = 0f; // TODO: is this the correct value?
        RigidBody.LinearVelocity = Vector3.Zero;
        RigidBody.AngularVelocity = Vector3.Zero;
        RigidBody.LinearFactor = Vector3.One;
        RigidBody.AngularFactor = Vector3.One;

        SetRotPos(Start.Position, Quaternion.Identity);
        if (Mass != Start.Mass)
        {
            Mass = Start.Mass;
        }

        if (IsVisible != Start.Visible)
        {
            ctx.SetVisible(Id, Start.Visible);
        }

        if (!IsFixed && Start.Fixed)
        {
            Fix(world);
        }
    }

    internal void Unfix(DynamicsWorld world)
    {
        if (!IsFixed)
        {
            if (RigidBody.IsInWorld)
            {
                RigidBody.Activate(true);
            }

            return;
        }

        bool waInWorld = RigidBody.IsInWorld;
        if (RigidBody.IsInWorld)
        {
            world.RemoveRigidBody(RigidBody);
        }

        RigidBody.Gravity = world.Gravity;

        Mass = _mass;

        if (waInWorld)
        {
            world.AddRigidBody(RigidBody);
            RigidBody.Activate(true);
        }

        IsFixed = false;
    }

    private void Fix(DynamicsWorld world)
    {
        if (IsFixed)
        {
            return;
        }

        bool wasInWorld = RigidBody.IsInWorld;
        if (wasInWorld)
        {
            world.RemoveRigidBody(RigidBody);
        }

        _mass = 0f;
        RigidBody.SetMassProps(0f, Vector3.Zero);
        RigidBody.UpdateInertiaTensor();

        RigidBody.Gravity = Vector3.Zero;

        if (wasInWorld)
        {
            world.AddRigidBody(RigidBody);
        }

        IsFixed = true;
    }

    /// <summary>
    /// Info about a collision.
    /// </summary>
    public readonly struct CollisionInfo
    {
        /// <summary>
        /// Default <see cref="CollisionInfo"/>.
        /// </summary>
        public static readonly CollisionInfo Default = new CollisionInfo(-1f, default, default);

        /// <summary>
        /// Initializes a new instance of the <see cref="CollisionInfo"/> struct.
        /// </summary>
        /// <param name="force">Force of the collision.</param>
        /// <param name="otherObject">Id of the other object.</param>
        /// <param name="normal">Normal of the collision.</param>
        public CollisionInfo(float force, FcObject otherObject, Vector3 normal)
        {
            Force = force;
            OtherObject = otherObject;
            Normal = normal;
        }

        /// <summary>
        /// Gets the force of the collision.
        /// </summary>
        /// <value>Force of the collision.</value>
        public float Force { get; }

        /// <summary>
        /// Gets the id of the other object.
        /// </summary>
        /// <value>Id of the other object.</value>
        public FcObject OtherObject { get; }

        /// <summary>
        /// Gets the normal of the collision.
        /// </summary>
        /// <value>Normal of the collision.</value>
        public Vector3 Normal { get; }
    }

    internal readonly struct StartValues
    {
        public StartValues(Vector3 position, float mass, bool visible, bool @fixed)
        {
            Position = position;
            Mass = mass;
            Visible = visible;
            Fixed = @fixed;
        }

        public readonly Vector3 Position { get; }

        public readonly float Mass { get; }

        public readonly bool Visible { get; }

        public readonly bool Fixed { get; }
    }
}
