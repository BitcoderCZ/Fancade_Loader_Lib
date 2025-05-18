using BulletSharp;
using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Raw;
using FancadeLoaderLib.Runtime.Bullet.Utils;
using FancadeLoaderLib.Runtime.Utils;
using MathUtils.Vectors;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib.Runtime.Bullet;

public sealed partial class FcWorld : IDisposable
{
    private readonly DiscreteDynamicsWorld _world;

    private readonly BulletRuntimeContext _runtimeCtx;

    private readonly IAstRunner _runner;

    private readonly CameraInfo _cameraInfo;

    private readonly RigidBody _groundPlane;

    private readonly PrefabList _prefabs;

    private readonly GameMeshInfo _gameMesh;

    private readonly List<RuntimeObject> _objects = [];

    private readonly List<Generic6DofSpring2Constraint> _constraints = [];

    private readonly Dictionary<FcObject, RuntimeObject> _idToObject = [];

    private readonly Dictionary<FcConstraint, Generic6DofSpring2Constraint> _idToConstraint = [];

    private readonly Dictionary<(ushort PrefabId, int3 Pos, byte3 VoxelPos), FcObject> _connectorToObject = [];

    private readonly Dictionary<(int Type, float3 Size), CollisionShape> _collisionShapeCache = [];

    private int _objectIdCounter = 1;

    private int _constraintIdCounter = 1;

    private int _disposed;

    private FcWorld(IRuntimeContextBase runtimeContext, Func<IRuntimeContext, IAstRunner> runnerFactory, PrefabList prefabs, ushort mainId)
    {
        _runtimeCtx = new BulletRuntimeContext(this, runtimeContext);
        _runner = runnerFactory(_runtimeCtx);
        _cameraInfo = new CameraInfo(new ScreenInfo(_runtimeCtx.ScreenSize.ToNumerics()));
        _prefabs = prefabs;

        var collisionConf = new DefaultCollisionConfiguration();
        var dispatcher = new CollisionDispatcher(collisionConf);
        var broadphase = new DbvtBroadphase();
        var solver = new SequentialImpulseConstraintSolver();
        _world = new DiscreteDynamicsWorld(dispatcher, broadphase, solver, collisionConf)
        {
            Gravity = new Vector3(0, -10, 0)
        };

        _groundPlane = _world.CreateStaticBody(Matrix4x4.Identity, new StaticPlaneShape(Vector3.UnitY, 0f));
        _groundPlane.UserIndex = FcObject.Null.Value;
        _groundPlane.Restitution = 1f;

        _gameMesh = GameMeshInfo.Create(prefabs, mainId);

        InitObjects(mainId);

        _world.UpdateAabbs();
    }

    public static FcWorld Create(ushort prefabId, PrefabList prefabs, IRuntimeContextBase runtimeContext, Func<IRuntimeContext, IAstRunner> runnerFactory)
    {
        ThrowIfNull(runtimeContext, nameof(runtimeContext));
        ThrowIfNull(runnerFactory, nameof(runnerFactory));

        if (prefabs.IdOffset != RawGame.CurrentNumbStockPrefabs)
        {
            ThrowArgumentException($"{nameof(prefabs)}.{nameof(prefabs.IdOffset)} must be equal to {nameof(RawGame)}.{nameof(RawGame.CurrentNumbStockPrefabs)}.", nameof(prefabs));
        }

        return new FcWorld(runtimeContext, runnerFactory, prefabs, prefabId);
    }

    public void RunFrame(float timeStep = 1f / 60f)
    {
        if (_disposed == 1)
        {
            throw new ObjectDisposedException(nameof(FcWorld));
        }

        _cameraInfo.Step(new ScreenInfo(_runtimeCtx.ScreenSize.ToNumerics())); // wts/stw are delayed by 1 frame

        var lateUpdate = _runner.RunFrame();

        foreach (var rObject in _objects)
        {
            rObject.MaxForceCollision = RuntimeObject.CollisionInfo.Default;
        }

        _world.StepSimulation(timeStep);

        int numManifolds = _world.Dispatcher.NumManifolds;
        for (int i = 0; i < numManifolds; i++)
        {
            var manifold = _world.Dispatcher.GetManifoldByIndexInternal(i);

            int numContacts = manifold.NumContacts;
            if (numContacts == 0)
            {
                continue;
            }

            float maxImpulse = 0f;
            ManifoldPoint? strongestPoint = null;

            for (int j = 0; j < numContacts; j++)
            {
                var pt = manifold.GetContactPoint(j);
                if (pt.AppliedImpulse > maxImpulse)
                {
                    maxImpulse = pt.AppliedImpulse;
                    strongestPoint = pt;
                }
            }

            // TODO: IsActive and Distance not needed originally, also AppliedImpulse seems to be higher with my impl, fancade uses custom collision algorithm so that might be the cause, but I can't replicate that with BulletSharp (without modifying it, which I don't/can't do); investigate why
            if (strongestPoint == null || maxImpulse < 0.1f || strongestPoint.Distance > -0.005f)
            {
                continue;
            }

            manifold.ClearManifold();

            var bodyA = manifold.Body0 as RigidBody;
            var bodyB = manifold.Body1 as RigidBody;

            int idA = bodyA?.UserIndex ?? -1;
            int idB = bodyB?.UserIndex ?? -1;

            Vector3 normalOnB = strongestPoint.NormalWorldOnB;

            if (idA != -1 && TryGetObject((FcObject)idA, out var rA) && rA.RigidBody.IsActive)
            {
                rA.MaxForceCollision = new RuntimeObject.CollisionInfo(maxImpulse, (FcObject)idB, normalOnB);
            }

            if (idB != -1 && TryGetObject((FcObject)idB, out var rB) && rB.RigidBody.IsActive)
            {
                rB.MaxForceCollision = new RuntimeObject.CollisionInfo(maxImpulse, (FcObject)idA, -normalOnB);
            }
        }

        foreach (var rObject in _objects)
        {
            rObject.Update();
        }

        lateUpdate();

        _runtimeCtx.CurrentFrame++;
    }

    private void InitObjects(ushort mainId)
    {
        var stockPrefabs = StockBlocks.PrefabList;
        var usedPrefabs = PrefabUsedCache.Create(_prefabs, mainId);

        foreach (var prefab in stockPrefabs.Concat(_prefabs))
        {
            if (!usedPrefabs.Used(prefab.Id) || (prefab.Id < RawGame.CurrentNumbStockPrefabs && StockIsScript.Data[prefab.Id]))
            {
                continue;
            }

            var meshInfo = _gameMesh.GetBlockMesh(prefab.Id);

            for (int i = 0; i < meshInfo.MeshCount; i++)
            {
                var objectId = (FcObject)_objectIdCounter++;
                short objectInPrefabMeshIndex = (short)i;

                var insideSize = prefab.Blocks.Array.Size;

                ushort[] blocks = prefab.Blocks.Array.Array;

                float totalVolume = 0f;
                float3 centerOfMass = float3.Zero;
                float3 sizeMin = new float3(float.MaxValue, float.MaxValue, float.MaxValue);
                float3 sizeMax = new float3(float.MinValue, float.MinValue, float.MinValue);

                bool foundPhysics = false;

                int index = 0;
                for (int z = 0; z < insideSize.Z; z++)
                {
                    for (int y = 0; y < insideSize.Y; y++)
                    {
                        for (int x = 0; x < insideSize.X; x++, index++)
                        {
                            ushort blockId = blocks[index];

                            if (blockId == 0)
                            {
                                continue;
                            }

                            var currentSegment = _prefabs.GetSegmentOrStock(blockId);
                            var currentPrefab = _prefabs.GetPrefabOrStock(currentSegment.PrefabId);
                            var currentSegmentMesh = _gameMesh.GetSegmentMesh(blockId);

                            if (currentPrefab.Type == PrefabType.Physics)
                            {
                                for (int meshIndex = 0; meshIndex < currentSegmentMesh.MeshCount; meshIndex++)
                                {
                                    if (meshInfo.BlockMeshIds[meshIndex + meshInfo.BlockMeshIdOffsets[index]] != objectInPrefabMeshIndex)
                                    {
                                        continue;
                                    }

                                    foundPhysics = true;

                                    var (boundsMin, boundsMax) = GetMeshBounds(currentSegment.Voxels, currentSegmentMesh, (byte)meshIndex);

                                    float3 size = (boundsMax - boundsMin + float3.One) * 0.125f;
                                    float volume = size.X * size.Y * size.Z;
                                    totalVolume += volume;

                                    float3 worldBoundsMin = (float3)boundsMin * 0.125f + new float3(x, y, z);
                                    float3 worldBoundsMax = (float3)boundsMax * 0.125f + new float3(x + 0.125f, y + 0.125f, z + 0.125f);

                                    centerOfMass += (size * 0.5f + worldBoundsMin) * volume;

                                    sizeMin = float3.Min(sizeMin, worldBoundsMin);
                                    sizeMax = float3.Max(sizeMax, worldBoundsMax);
                                }
                            }
                            else
                            {
                                for (int meshIndex = 0; meshIndex < currentSegmentMesh.MeshCount; meshIndex++)
                                {
                                    if (meshInfo.BlockMeshIds[meshIndex + meshInfo.BlockMeshIdOffsets[index]] != objectInPrefabMeshIndex)
                                    {
                                        continue;
                                    }

                                    centerOfMass += new float3(x, y, z) + 0.5f;
                                    totalVolume++;

                                    sizeMin = float3.Min(sizeMin, new float3(x, y, z));
                                    sizeMax = float3.Max(sizeMax, new float3(x + 1f, y + 1f, z + 1f));

                                    break;
                                }
                            }
                        }
                    }
                }

                float3 pos = centerOfMass * 1f / ((totalVolume == 0.0f) ? 1.0f : totalVolume);
                float mass = totalVolume;

                sizeMin -= pos;
                sizeMax -= pos;

                var rigidBody = BulletCreate(pos.ToNumerics(), Quaternion.Identity, objectId);
                rigidBody.UpdateInertiaTensor();

                RuntimeObject rObject = new(objectId, prefab.Id, objectInPrefabMeshIndex, rigidBody, pos, Quaternion.Identity, sizeMin, sizeMax, mass)
                {
                    InOpenLevel = prefab.Id == mainId,
                };

                if (foundPhysics)
                {
                    rObject.Unfix(_world);
                }

                _objects.Add(rObject);
                _idToObject.Add(rObject.Id, rObject);

                if (rObject.InOpenLevel)
                {
                    _world.AddRigidBody(rObject.RigidBody);
                }

                AddColliders(rObject);
            }
        }

        InitConnectedObjects(usedPrefabs);
    }

    private void InitConnectedObjects(PrefabUsedCache usedPrefabs)
    {
        var stockPrefabs = StockBlocks.PrefabList;

        var terminalInfos = PrefabTerminalInfo.Create(stockPrefabs.Concat(_prefabs));

        foreach (var prefab in stockPrefabs.Concat(_prefabs))
        {
            if (!usedPrefabs.Used(prefab.Id) ||
                (prefab.Id < RawGame.CurrentNumbStockPrefabs && StockIsScript.Data[prefab.Id]) ||
                prefab.Connections.Count == 0)
            {
                continue;
            }

            var meshInfo = _gameMesh.GetBlockMesh(prefab.Id);

            foreach (var connection in prefab.Connections)
            {
                if (connection.IsFromOutside)
                {
                    continue;
                }

                ushort blockId = prefab.Blocks.GetBlock(connection.From);
                ushort segmentId = prefab.Blocks.GetBlock(connection.From + connection.FromVoxel / 8);

                if (blockId == 0 || segmentId == 0)
                {
                    continue;
                }

                var terminalInfo = terminalInfos[blockId];
                var segmentMeshes = _gameMesh.GetSegmentMesh(segmentId);

                if (!terminalInfo.OutputTerminals.Any(terminal => terminal.Position == connection.FromVoxel))
                {
                    int meshIndex = meshInfo.BlockMeshIds[segmentMeshes.VoxelMeshIndex[PrefabSegment.IndexVoxels(connection.FromVoxel % 8)] + meshInfo.BlockMeshIdOffsets[((int3)connection.From).ToIndex(prefab.Blocks.Size.X, prefab.Blocks.Size.Y)]];

                    var obj = _objects.FirstOrDefault(obj => obj.OutsidePrefabId == prefab.Id && obj.InPrefabMeshIndex == meshIndex);

                    if (obj is not null)
                    {
#if RELEASE
                        _connectorToObject[(prefab.Id, connection.From, (byte3)connection.FromVoxel)] = obj.Id;
#else
                        if (!_connectorToObject.TryAdd((prefab.Id, connection.From, (byte3)connection.FromVoxel), obj.Id))
                        {
                            Debug.Assert(_connectorToObject[(prefab.Id, connection.From, (byte3)connection.FromVoxel)] == obj.Id);
                        }
#endif
                    }
                }
            }
        }
    }

    private static RigidBody BulletCreate(Vector3 position, Quaternion rotation, FcObject id)
    {
        CompoundShape shape = new CompoundShape(true, 0);

        var motionState = new DefaultMotionState(Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateTranslation(position));

        Vector3 localInertia = shape.CalculateLocalInertia(0f);

        RigidBody body;
        using (var rbInfo = new RigidBodyConstructionInfo(0f, motionState, shape, localInertia)
        {
            Friction = 0.5f,
        })
        {
            body = new RigidBody(rbInfo);
        }

        body.UserIndex = id.Value;

        return body;
    }

    private static (short3 Min, short3 Max) GetMeshBounds(Voxel[]? voxels, PrefabSegmentMeshes mesh, byte meshIndex)
    {
        short3 min = new short3(short.MaxValue, short.MaxValue, short.MaxValue);
        short3 max = new short3(short.MinValue, short.MinValue, short.MinValue);

        if (voxels is null)
        {
            goto SkipLoop;
        }

        Debug.Assert(voxels.Length == 8 * 8 * 8);

        int voxelIndex = 0;
        for (int z = 0; z < 8; z++)
        {
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++, voxelIndex++)
                {
                    if (voxels[voxelIndex].IsEmpty || mesh.VoxelMeshIndex[voxelIndex] != meshIndex)
                    {
                        continue;
                    }

                    short3 pos = new short3(x, y, z);

                    min = short3.Min(min, pos);
                    max = short3.Max(max, pos);
                }
            }
        }

    SkipLoop:
        // no voxels
        if (min.X == short.MaxValue)
        {
            min = new short3(0);
            max = new short3(-1);
        }

        return (min, max);
    }

    private void AddColliders(RuntimeObject rObject)
    {
        var prefab = _prefabs.GetPrefabOrStock(rObject.OutsidePrefabId);
        var blockMesh = _gameMesh.GetBlockMesh(rObject.OutsidePrefabId);

        var insideSize = prefab.Blocks.Array.Size;
        int insideLength = insideSize.X * insideSize.Y * insideSize.Z;

        ushort[] blocks = prefab.Blocks.Array.Array;
        Debug.Assert(blocks.Length == insideLength);
        for (int insideIndex = 0; insideIndex < insideLength; insideIndex++)
        {
            ushort blockId = blocks[insideIndex];

            if (blockId == 0)
            {
                continue;
            }

            var currentSegment = _prefabs.GetSegmentOrStock(blockId);
            var currentPrefab = _prefabs.GetPrefabOrStock(currentSegment.PrefabId);
            var currentSegmentMeshes = _gameMesh.GetSegmentMesh(blockId);

            if (currentPrefab.Collider == PrefabCollider.None || currentSegmentMeshes.MeshCount == 0)
            {
                continue;
            }

            var currentPos = int3.FromIndex(insideIndex, insideSize.X, insideSize.Y);

            for (int meshIndex = 0; meshIndex < currentSegmentMeshes.MeshCount; meshIndex++)
            {
                if (blockMesh.BlockMeshIds[meshIndex + blockMesh.BlockMeshIdOffsets[insideIndex]] != rObject.InPrefabMeshIndex)
                {
                    continue;
                }

                var (boundsMin, boundsMax) = GetMeshBounds(currentSegment.Voxels, currentSegmentMeshes, (byte)meshIndex);

                float3 size = (float3)((boundsMax - boundsMin) + int3.One) * 0.125f;

                float3 offset = ((size * 0.5f) + ((float3)boundsMin * 0.125f) + currentPos) - rObject.StartPos;

                uint connectsToSideBitfield = 0;
                int colliderType;
                switch (currentPrefab.Collider)
                {
                    case PrefabCollider.Sphere:
                        {
                            colliderType = 2;
                            float sizeMax = MathF.Max(MathF.Max(size.X, size.Y), size.Z);
                            size = new float3(sizeMax, sizeMax, sizeMax);
                            connectsToSideBitfield = 0;
                        }

                        break;
                    case PrefabCollider.Box:
                        {
                            for (int sideIndex = 0; sideIndex < 6; sideIndex++)
                            {
                                int3 neighborPos = new int3(-1, -1, -1);
                                bool neighborIsAnotherBlock;

                                switch (sideIndex)
                                {
                                    case 0:
                                        if (currentPos.X < insideSize.X - 1)
                                        {
                                            neighborPos = currentPos + new int3(1, 0, 0);
                                        }

                                        neighborIsAnotherBlock = boundsMax.X == 7;
                                        break;
                                    case 1:
                                        if (currentPos.X > 1)
                                        {
                                            neighborPos = currentPos + new int3(-1, 0, 0);
                                        }

                                        neighborIsAnotherBlock = boundsMin.X == 0;
                                        break;
                                    case 2:
                                        if (currentPos.Y < insideSize.Y - 1)
                                        {
                                            neighborPos = currentPos + new int3(0, 1, 0);
                                        }

                                        neighborIsAnotherBlock = boundsMax.Y == 7;
                                        break;
                                    case 3:
                                        if (currentPos.Y > 1)
                                        {
                                            neighborPos = currentPos + new int3(0, -1, 0);
                                        }

                                        neighborIsAnotherBlock = boundsMin.Y == 0;
                                        break;
                                    case 4:
                                        if (currentPos.Z < insideSize.Z - 1)
                                        {
                                            neighborPos = currentPos + new int3(0, 0, 1);
                                        }

                                        neighborIsAnotherBlock = boundsMax.Z == 7;
                                        break;
                                    default:
                                        Debug.Assert(sideIndex == 5);
                                        if (currentPos.Z > 1)
                                        {
                                            neighborPos = currentPos + new int3(0, 0, -1);
                                        }

                                        neighborIsAnotherBlock = boundsMin.Z == 0;
                                        break;
                                }

                                if (neighborPos == new int3(-1, -1, -1) || !neighborIsAnotherBlock)
                                {
                                    continue;
                                }

                                int neighborIndex = neighborPos.ToIndex(insideSize.X, insideSize.Y);
                                ushort neighborId = blocks[neighborIndex];

                                if (neighborId == 0 || _prefabs.GetPrefabOrStock(_prefabs.GetSegmentOrStock(neighborId).PrefabId).Collider != PrefabCollider.Box)
                                {
                                    continue;
                                }

                                var neighborSegmentMeshes = _gameMesh.GetSegmentMesh(neighborId);

                                if (neighborSegmentMeshes.MeshCount > 0)
                                {
                                    int neighborMeshIndexToUse = -1;
                                    for (int neighborMeshIndex = 0; neighborMeshIndex < neighborSegmentMeshes.MeshCount; neighborMeshIndex++)
                                    {
                                        if (blockMesh.BlockMeshIds[neighborMeshIndex + blockMesh.BlockMeshIdOffsets[neighborIndex]] == rObject.InPrefabMeshIndex)
                                        {
                                            var (neighborBoundsMin, neighborBoundsMax) = GetMeshBounds(_prefabs.GetSegmentOrStock(neighborId).Voxels, neighborSegmentMeshes, (byte)neighborMeshIndex);

                                            if (sideIndex < 6)
                                            {
                                                int sideShifted = 1 << sideIndex;

                                                if ((sideShifted & 0b11) == 0)
                                                {
                                                    if ((sideShifted & 0b1100) == 0)
                                                    {
                                                        if ((((boundsMin.X == neighborBoundsMin.X) &&
                                                            (boundsMax.X == neighborBoundsMax.X)) &&
                                                            (boundsMin.Y == neighborBoundsMin.Y)) &&
                                                            (boundsMax.Y == neighborBoundsMax.Y))
                                                        {
                                                            neighborMeshIndexToUse = neighborMeshIndex;
                                                        }
                                                    }
                                                    else if ((boundsMin.X == neighborBoundsMin.X) &&
                                                        (boundsMax.X == neighborBoundsMax.X))
                                                    {
                                                        if ((boundsMin.Z == neighborBoundsMin.Z) &&
                                                            (boundsMax.Z == neighborBoundsMax.Z))
                                                        {
                                                            neighborMeshIndexToUse = neighborMeshIndex;
                                                        }
                                                    }
                                                }
                                                else if ((boundsMin.Y == neighborBoundsMin.Y) &&
                                                    (boundsMax.Y == neighborBoundsMax.Y))
                                                {
                                                    if ((boundsMin.Z == neighborBoundsMin.Z) &&
                                                       (boundsMax.Z == neighborBoundsMax.Z))
                                                    {
                                                        neighborMeshIndexToUse = neighborMeshIndex;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                neighborMeshIndexToUse = neighborMeshIndex;
                                            }
                                        }
                                    }

                                    if (neighborMeshIndexToUse != -1)
                                    {
                                        if (currentSegmentMeshes.Meshes[meshIndex].Bitfields[sideIndex] == neighborSegmentMeshes.Meshes[neighborMeshIndexToUse].Bitfields[(int)((sideIndex & 0xffffffff) ^ 1)])
                                        {
                                            connectsToSideBitfield |= (uint)(1L << (sideIndex & 0b111111));
                                        }
                                    }
                                }
                            }

                            colliderType = 1;
                        }

                        break;
                    default:
                        {
                            Debug.Assert(currentPrefab.Collider == PrefabCollider.None);
                            colliderType = 3;
                            connectsToSideBitfield = 0;
                        }

                        break;
                }

                if (!_collisionShapeCache.TryGetValue((colliderType, size), out var shape))
                {
                    switch (colliderType)
                    {
                        case 1:
                            shape = new BoxShape(size.ToNumerics() * 0.5f);
                            break;
                        case 2:
                            shape = new SphereShape(size.X * 0.5f);
                            break;
                        default:
                            continue;
                    }

                    _collisionShapeCache.Add((colliderType, size), shape);
                }

                ((CompoundShape)rObject.RigidBody.CollisionShape).AddChildShape(Matrix4x4.CreateTranslation(offset.ToNumerics()), shape);
            }
        }
    }

    private bool TryGetObject(FcObject @object, [MaybeNullWhen(false)] out RuntimeObject rObject)
    {
        if (@object == FcObject.Null)
        {
            rObject = null;
            return false;
        }

        return _idToObject.TryGetValue(@object, out rObject);
    }

    private bool TryGetConstraint(FcConstraint constraint, [MaybeNullWhen(false)] out Generic6DofSpring2Constraint bConstraint)
    {
        if (constraint == FcConstraint.Null)
        {
            bConstraint = null;
            return false;
        }

        return _idToConstraint.TryGetValue(constraint, out bConstraint);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
        {
            return;
        }

        foreach (var shape in _collisionShapeCache)
        {
            shape.Value.Dispose();
        }

        foreach (var obj in _objects)
        {
            obj.Dispose();
        }

        foreach (var con in _constraints)
        {
            con.Dispose();
        }

        _groundPlane.Dispose();

        _world.Dispose();
    }
}
