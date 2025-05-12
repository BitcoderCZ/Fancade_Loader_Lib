using BulletSharp;
using FancadeLoaderLib.Runtime.Utils;
using MathUtils.Vectors;
using System.Diagnostics;
using System.Numerics;

namespace FancadeLoaderLib.Runtime.Bullet;

public sealed class RuntimeObject : IDisposable
{
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
        Mass = mass;
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

    public float Mass { get; }

    public bool InOpenLevel { get; init; }

    public bool UserCreated => InPrefabMeshIndex == -1;

    public bool IsVisible { get; set; } = true;

    public bool IsFixed { get; set; } = true;

    public void Update()
    {
        Debug.Assert(RigidBody.MotionState is not null);

        var wt = RigidBody.MotionState.WorldTransform;

        Pos = wt.Translation.ToFloat3();
        Rot = wt.GetRotation();
    }

    public void Dispose()
        => RigidBody.Dispose();
}
