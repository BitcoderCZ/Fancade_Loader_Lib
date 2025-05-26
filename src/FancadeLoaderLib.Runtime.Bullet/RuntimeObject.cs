using BulletSharp;
using FancadeLoaderLib.Runtime.Utils;
using MathUtils.Vectors;
using System.Diagnostics;
using System.Numerics;

namespace FancadeLoaderLib.Runtime.Bullet;

public sealed class RuntimeObject : IDisposable
{
    private float _mass = 1f;

    public RuntimeObject(FcObject id, ushort outsidePrefabId, short inPrefabMeshIndex, RigidBody rigidBody, float3 pos, Quaternion rot, float3 sizeMin, float3 sizeMax, float mass)
    {
        Debug.Assert(id != FcObject.Null);
        Debug.Assert(inPrefabMeshIndex >= -1);
        Id = id;
        OutsidePrefabId = outsidePrefabId;
        InPrefabMeshIndex = inPrefabMeshIndex;
        RigidBody = rigidBody;
        Pos = pos;
        StartPos = Pos;
        Rot = rot;
        SizeMin = sizeMin;
        SizeMax = sizeMax;
        _mass = mass;
    }

    private RuntimeObject(FcObject id, ushort outsidePrefabId, short inPrefabMeshIndex, RigidBody rigidBody, float3 pos, float3 startPos, Quaternion rot, float3 sizeMin, float3 sizeMax, float mass, bool isVisible, bool isUserCreated)
    {
        Id = id;
        OutsidePrefabId = outsidePrefabId;
        InPrefabMeshIndex = inPrefabMeshIndex;
        RigidBody = rigidBody;
        Pos = pos;
        StartPos = startPos;
        Rot = rot;
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

    public float3 Pos { get; private set; }

    public float3 StartPos { get; }

    public Quaternion Rot { get; private set; }

    public float3 SizeMin { get; }

    public float3 SizeMax { get; }

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

        Pos = wt.Translation.ToFloat3();
        Rot = wt.GetRotation();
    }

    public void SetRotPos(float3? position, Quaternion? rotation)
    {
        Debug.Assert(RigidBody.MotionState is not null);
        var mat = RigidBody.MotionState.WorldTransform;

        if (position is { } pos)
        {
            Pos = pos;
            mat.Translation = pos.ToNumerics();
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

    public RuntimeObject Clone(FcObject newId, RigidBody newBody, bool userCreated)
    {
        var newObject = new RuntimeObject(newId, OutsidePrefabId, InPrefabMeshIndex, newBody, Pos + float3.One, StartPos, Rot, SizeMin, SizeMax, Mass, true, userCreated);

        return newObject;
    }

    public void Dispose()
    {
        RigidBody.MotionState?.Dispose();
        RigidBody.Dispose();
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
