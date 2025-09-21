using BitcoderCZ.BulletSharp;
using System.Diagnostics;
using System.Numerics;

namespace BitcoderCZ.Fancade.Runtime.Simulated.Bullet;

public sealed class RuntimeObject : IDisposable
{
    private float _mass = 1f;

    public RuntimeObject(FcObject id, ushort outsidePrefabId, short inPrefabMeshIndex, RigidBody rigidBody, Vector3 pos, Quaternion rot, Vector3 sizeMin, Vector3 sizeMax, float mass, bool visible, bool @fixed)
    {
        Debug.Assert(id != FcObject.Null);
        Debug.Assert(inPrefabMeshIndex >= -1);
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

    public FcObject Id { get; }

    public ushort OutsidePrefabId { get; }

    public short InPrefabMeshIndex { get; }

    public RigidBody RigidBody { get; }

    public Vector3 Pos { get; private set; }

    public Quaternion Rot { get; private set; }

    public StartValues Start { get; }

    public Vector3 SizeMin { get; }

    public Vector3 SizeMax { get; }

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

    public bool IsUserCreated { get; init; } = false;

    public bool IsVisible { get; set; } = true;

    public bool IsFixed { get; private set; } = true;

    public CollisionInfo MaxForceCollision { get; set; } = CollisionInfo.Default;

    public void Update()
    {
        Debug.Assert(RigidBody.MotionState is not null);

        // for some reason in some situations not updated
        //var wt = RigidBody.MotionState.WorldTransform;
        //Debug.Assert(wt == RigidBody.WorldTransform);
        var wt = RigidBody.WorldTransform;

        Pos = wt.Translation;
        Rot = wt.GetRotation();
    }

    public void SetRotPos(Vector3? position, Quaternion? rotation)
    {
        Debug.Assert(RigidBody.MotionState is not null);
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

    public void Unfix(DynamicsWorld world)
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

    public RuntimeObject Clone(FcObject newId, RigidBody newBody, bool userCreated)
    {
        var newObject = new RuntimeObject(newId, OutsidePrefabId, InPrefabMeshIndex, newBody, Pos + Vector3.One, Rot, Start, SizeMin, SizeMax, Mass, true, userCreated);

        return newObject;
    }

    public void Dispose()
    {
        RigidBody.MotionState?.Dispose();
        RigidBody.Dispose();
    }

    public readonly struct StartValues
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

    public readonly struct CollisionInfo
    {
        public static readonly CollisionInfo Default = new CollisionInfo(-1f, default, default);

        public CollisionInfo(float force, FcObject otherObject, Vector3 normal)
        {
            Force = force;
            OtherObject = otherObject;
            Normal = normal;
        }

        public float Force { get; }

        public FcObject OtherObject { get; }

        public Vector3 Normal { get; }
    }
}
