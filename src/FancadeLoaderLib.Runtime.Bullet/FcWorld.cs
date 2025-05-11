using BulletSharp;
using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Raw;
using FancadeLoaderLib.Runtime.Bullet.Utils;
using FancadeLoaderLib.Runtime.Utils;
using MathUtils.Vectors;
using System.Diagnostics;
using System.Numerics;
using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib.Runtime.Bullet;

public sealed partial class FcWorld : IDisposable
{
    private readonly DiscreteDynamicsWorld _world;

    private readonly BulletRuntimeContext _runtimeCtx;

    private readonly IAstRunner _runner;

    private readonly RigidBody _groundPlane;

    private readonly PrefabList _prefabs;

    private readonly GameMeshInfo _gameMesh;

    private readonly List<RuntimeObject> _objects = [];

    private readonly Dictionary<FcObject, RuntimeObject> _idToObject = [];

    private int _objectIdCounter = 1;

    private int _disposed;

    private FcWorld(IRuntimeContextBase runtimeContext, Func<IRuntimeContext, IAstRunner> runnerFactory, PrefabList prefabs, ushort mainId)
    {
        _runtimeCtx = new BulletRuntimeContext(this, runtimeContext);
        _runner = runnerFactory(_runtimeCtx);
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
        _groundPlane.Restitution = 1f;

        _gameMesh = GameMeshInfo.Create(prefabs, mainId);

        InitObjects(mainId);
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

        var lateUpdate = _runner.RunFrame();

        _world.StepSimulation(timeStep);

        lateUpdate();

        _runtimeCtx.CurrentFrame++;
    }

    public RuntimeObject? GetObject(FcObject @object)
        => @object == FcObject.Null || !_idToObject.TryGetValue(@object, out var rObject)
        ? null
        : rObject;

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

                var rigidBody = BulletCreate(pos.ToNumerics(), Quaternion.Identity, mass, objectId);

                RuntimeObject rObject = new(objectId, prefab.Id, objectInPrefabMeshIndex, rigidBody, pos, Quaternion.Identity, sizeMin, sizeMax, mass)
                {
                    InOpenLevel = prefab.Id == mainId,
                };

                _objects.Add(rObject);
                _idToObject.Add(rObject.Id, rObject);

                if (rObject.InOpenLevel)
                {
                    _world.AddRigidBody(rObject.RigidBody);
                }

                AddColliders(rObject);
            }
        }
    }

    private static RigidBody BulletCreate(Vector3 position, Quaternion rotation, float mass, FcObject id)
    {
        Debug.Assert(mass > 0f);

        CompoundShape shape = new CompoundShape(true, 0);

        var motionState = new DefaultMotionState(Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateTranslation(position));

        Vector3 localInertia = shape.CalculateLocalInertia(mass);

        RigidBody body;
        using (var rbInfo = new RigidBodyConstructionInfo(mass, motionState, shape, localInertia))
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

                // TODO: reuse shapes

                CollisionShape shape;
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

                ((CompoundShape)rObject.RigidBody.CollisionShape).AddChildShape(Matrix4x4.CreateTranslation(offset.ToNumerics()), shape);
            }
        }
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

        _world.Dispose();
        _groundPlane.Dispose();

        GC.SuppressFinalize(this);
    }
}
