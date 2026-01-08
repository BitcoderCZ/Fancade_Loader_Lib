// <copyright file="FcWorld.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.BulletSharp.Collision.BroadphaseCollision;
using BitcoderCZ.BulletSharp.Collision.CollisionDispatch;
using BitcoderCZ.BulletSharp.Collision.CollisionShapes;
using BitcoderCZ.BulletSharp.Collision.NarrowPhaseCollision;
using BitcoderCZ.BulletSharp.Dynamics;
using BitcoderCZ.BulletSharp.Dynamics.Constraints;
using BitcoderCZ.BulletSharp.LinearMath;
using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Fancade.Raw;
using BitcoderCZ.Fancade.Runtime.Exceptions;
using BitcoderCZ.Fancade.Runtime.Simulated.Bullet.Utils;
using BitcoderCZ.Fancade.Runtime.Simulated.Utils;
using BitcoderCZ.Fancade.Utils;
using BitcoderCZ.Maths.Vectors;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;
using static BitcoderCZ.Fancade.Utils.ThrowHelper;

namespace BitcoderCZ.Fancade.Runtime.Simulated.Bullet;

/// <summary>
/// A simulated fancade level.
/// </summary>
public sealed partial class FcWorld : IAstRunner
{
    private readonly DiscreteDynamicsWorld _world;

    private BulletRuntimeContext _runtimeCtx;

    private readonly IAstRunner _runner;

    private readonly RigidBody _groundPlane;

    private readonly ushort _mainPrefab;

    private readonly PrefabList _prefabs;

    private GameMeshInfo _gameMesh;

    private readonly List<RuntimeObject> _objects = [];

    private readonly List<Generic6DofSpring2Constraint> _constraints = [];

    private readonly Dictionary<FcObject, RuntimeObject> _idToObject = [];

    private readonly Dictionary<FcConstraint, Generic6DofSpring2Constraint> _idToConstraint = [];

    private readonly Dictionary<(ushort PrefabId, int3 Pos, byte3 VoxelPos), FcObject> _connectorToObject = [];

    private readonly Dictionary<(int Type, Vector3 Size), CollisionShape> _collisionShapeCache = [];

    private int _maximumObjectCount = 4096;

    private int _objectIdCounter = 1;

    private int _constraintIdCounter = 1;

    private int _disposed;

    private FcWorld(IRuntimeContextBase runtimeContext, Func<IRuntimeContext, IAstRunner> runnerFactory, PrefabList prefabs, ushort mainId, bool createMultiThreaded)
    {
        _runtimeCtx = new BulletRuntimeContext(this, runtimeContext);
        _runner = runnerFactory(_runtimeCtx);
        _prefabs = prefabs;
        _mainPrefab = mainId;

        var collisionConf = new DefaultCollisionConfiguration(new());
        var dispatcher = new CollisionDispatcher(collisionConf);
        var broadphase = new DbvtBroadphase();
        var solver = new SequentialImpulseConstraintSolver();
        _world = new DiscreteDynamicsWorld(dispatcher, broadphase, solver, collisionConf)
        {
            Gravity = new Vector3(0, -10, 0),
        };

        _groundPlane = _world.CreateStaticBody(Matrix4x4.Identity, new StaticPlaneShape(Vector3.UnitY, 0f));
        _groundPlane.UserIndex = FcObject.Null.Value;
        _groundPlane.Restitution = 1f;

        _gameMesh = GameMeshInfo.Create(prefabs, mainId, createMultiThreaded);

        InitObjects(mainId, createMultiThreaded);

        _world.UpdateAabbs();
    }

    /// <summary>
    /// An event raised when an object is cloned, first <see cref="RuntimeObject"/> is the original, the second is the clone.
    /// </summary>
    public event Action<RuntimeObject, RuntimeObject>? OnObjectCreated;

    /// <summary>
    /// An event raised when an object is destroyed.
    /// </summary>
    public event Action<RuntimeObject>? OnObjectDestroyed;

    /// <summary>
    /// Gets the <see cref="Simulated.GameMeshInfo"/> for the <see cref="FcWorld"/>.
    /// </summary>
    /// <value><see cref="Simulated.GameMeshInfo"/> for the <see cref="FcWorld"/>.</value>
    public ref readonly GameMeshInfo GameMeshInfo => ref _gameMesh;

    /// <summary>
    /// Gets the objects as a <see cref="IEnumerable{T}"/>.
    /// </summary>
    /// <value>Objects as a <see cref="IEnumerable{T}"/>.</value>
    public IEnumerable<RuntimeObject> Objects => _objects;

    /// <summary>
    /// Gets the objects as a <see cref="ReadOnlySpan{T}"/>.
    /// </summary>
    /// <value>Objects as a <see cref="ReadOnlySpan{T}"/>.</value>
    public ReadOnlySpan<RuntimeObject> ObjectsSpan => CollectionsMarshal.AsSpan(_objects);

    /// <summary>
    /// Gets the <see cref="DiscreteDynamicsWorld"/>.
    /// </summary>
    /// <value>The <see cref="DiscreteDynamicsWorld"/> used by the <see cref="FcWorld"/>.</value>
    public DiscreteDynamicsWorld BulletWorld => _world;

    /// <inheritdoc/>
    public IEnumerable<Variable> GlobalVariables => _runner.GlobalVariables;

    /// <inheritdoc/>
    public int EnvironmentCount => _runner.EnvironmentCount;

    /// <summary>
    /// Gets or sets the maximum allowed amount of user create objects, 4096 by default.
    /// </summary>
    /// <value>The maximum amount of objects created using <see cref="IRuntimeContext.CreateObject(FcObject, EnvironmentPosition)"/> before <see cref="TooManyObjectsException"/> is thrown.</value>
    public int MaximumObjectCount
    {
        get => _maximumObjectCount;
        set
        {
            ThrowHelper.ThrowIfNegative(value);

            _maximumObjectCount = value;
        }
    }

    /// <summary>
    /// Creates a new instance of the <see cref="FcWorld"/> class.
    /// </summary>
    /// <param name="prefabId">The open prefab.</param>
    /// <param name="prefabs">The game's prefabs.</param>
    /// <param name="runtimeContext">The <see cref="IRuntimeContextBase"/> to use.</param>
    /// <param name="runnerFactory">A func to create a <see cref="IAstRunner"/> given a <see cref="IRuntimeContext"/>.</param>
    /// <param name="createMultiThreaded">Whether to use multiple threads to create the <see cref="FcWorld"/>.</param>
    /// <returns>The created <see cref="FcWorld"/>.</returns>
    public static FcWorld Create(ushort prefabId, PrefabList prefabs, IRuntimeContextBase runtimeContext, Func<IRuntimeContext, IAstRunner> runnerFactory, bool createMultiThreaded = true)
    {
        ThrowIfNull(runtimeContext, nameof(runtimeContext));
        ThrowIfNull(runnerFactory, nameof(runnerFactory));

        if (prefabs.IdOffset != RawGame.CurrentNumbStockPrefabs)
        {
            ThrowArgumentException($"{nameof(prefabs)}.{nameof(prefabs.IdOffset)} must be equal to {nameof(RawGame)}.{nameof(RawGame.CurrentNumbStockPrefabs)}.", nameof(prefabs));
        }

        return new FcWorld(runtimeContext, runnerFactory, prefabs, prefabId, createMultiThreaded);
    }

    /// <summary>
    /// Runs a single frame.
    /// </summary>
    /// <param name="timeStep">The time that has passed since the last frame, 1 / 60 by default.</param>
    /// <exception cref="ObjectDisposedException">Thrown when the <see cref="FcWorld"/> instance has been disposed.</exception>
    /// <exception cref="FcTimeoutException">Thrown if the execution takes too long.</exception>
    /// <exception cref="InvalidInputException">Thrown when a script block receives invalid input.</exception>
    public void RunFrame(float timeStep = 1f / 60f)
    {
        if (_disposed == 1)
        {
            throw new ObjectDisposedException(nameof(FcWorld));
        }

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

            // TODO: IsActive and Distance not needed originally, also AppliedImpulse seems to be higher with my impl, fancade uses custom collision algorithm so that might be the cause, but I can't replicate that with BulletSharp (without modifying it, which I don't want to/can't do); investigate why
            if (strongestPoint == null || maxImpulse < 0.1f || strongestPoint.Distance > -0.005f)
            {
                continue;
            }

            manifold.ClearManifold();

            var bodyA = manifold.Body0 as RigidBody;
            var bodyB = manifold.Body1 as RigidBody;

            int idA = bodyA?.UserIndex ?? -1;
            int idB = bodyB?.UserIndex ?? -1;

            Vector3 normalOnB = strongestPoint._normalWorldOnB;

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

    /// <inheritdoc/>
    /// <exception cref="ObjectDisposedException">Thrown when the <see cref="FcWorld"/> instance has been disposed.</exception>
    public Action RunFrame()
    {
        const float TimeStep = 1f / 60f;

        if (_disposed == 1)
        {
            throw new ObjectDisposedException(nameof(FcWorld));
        }

        var lateUpdate = _runner.RunFrame();

        foreach (var rObject in _objects)
        {
            rObject.MaxForceCollision = RuntimeObject.CollisionInfo.Default;
        }

        _world.StepSimulation(TimeStep);

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

            // TODO: IsActive and Distance not needed originally, also AppliedImpulse seems to be higher with my impl, fancade uses custom collision algorithm so that might be the cause, but I can't replicate that with BulletSharp (without modifying it, which I don't want to/can't do); investigate why
            if (strongestPoint == null || maxImpulse < 0.1f || strongestPoint.Distance > -0.005f)
            {
                continue;
            }

            manifold.ClearManifold();

            var bodyA = manifold.Body0 as RigidBody;
            var bodyB = manifold.Body1 as RigidBody;

            int idA = bodyA?.UserIndex ?? -1;
            int idB = bodyB?.UserIndex ?? -1;

            Vector3 normalOnB = strongestPoint._normalWorldOnB;

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

        return () =>
        {
            lateUpdate();

            _runtimeCtx.CurrentFrame++;
        };
    }

    /// <inheritdoc/>
    public void Reset()
    {
        _runner.Reset();

        for (int i = 0; i < _objects.Count; i++)
        {
            var rObject = _objects[i];

            if (rObject.IsUserCreated)
            {
                _runtimeCtx.DestroyObject(rObject.Id);
            }
            else
            {
                rObject.Reset(_world, _runtimeCtx);
            }
        }

        _world.Gravity = new Vector3(0, -10, 0);

        _runtimeCtx.CurrentFrame = 0;

        for (int i = 0; i < _constraints.Count; i++)
        {
            var con = _constraints[i];
            Debug.Assert(con.UserConstraintPtr is FcConstraint, $"The UserConstraintPtr should be a {nameof(FcConstraint)}.");

            _idToConstraint.Remove((FcConstraint)con.UserConstraintPtr);
            _world.RemoveConstraint(con);
        }

        _constraints.Clear();
    }

    /// <inheritdoc/>
    public Span<RuntimeValue> GetGlobalVariableValue(Variable variable)
        => _runner.GetGlobalVariableValue(variable);

    /// <inheritdoc/>
    public IFcEnvironment GetEnvironment(int index)
        => _runner.GetEnvironment(index);

    /// <inheritdoc/>
    public void Dispose()
        => _runtimeCtx = null!;

    private static RigidBody BulletCreate(Vector3 position, Quaternion rotation, FcObject id)
    {
        var motionState = new DefaultMotionState(Transform.FromMatrix4x4(Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateTranslation(position)), Transform.Identity);

        RigidBody body;
        var rbInfo = new RigidBodyConstructionInfo(0f, motionState, null!, Vector3.Zero)
        {
            Friction = 0.5f,
        };

        body = new RigidBody(in rbInfo);

        body.UserIndex = id.Value;

        return body;
    }

    /*private static (short3 Min, short3 Max) GetMeshBounds(Voxels voxels, PrefabSegmentMeshes mesh, byte meshIndex)
    {
        short3 min = new short3(short.MaxValue, short.MaxValue, short.MaxValue);
        short3 max = new short3(short.MinValue, short.MinValue, short.MinValue);

        if (voxels.IsEmpty)
        {
            goto SkipLoop;
        }

        int voxelIndex = 0;
        for (int z = 0; z < 8; z++)
        {
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++, voxelIndex++)
                {
                    if (voxels.GetRawFace(voxelIndex) == 0 || mesh.VoxelMeshIndex[voxelIndex] != meshIndex)
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
    }*/

    private void InitObjects(ushort mainId, bool createMultiThreaded)
    {
        var stockPrefabs = StockBlocks.PrefabList;
        var usedPrefabs = PrefabUsedCache.Create(_prefabs, mainId);

        var uniqueMeshInfo = new (float TotalVolume, Vector3 Position, Vector3 SizeMin, Vector3 SizeMax, bool FoundPhysics, CompoundShape Shape)?[_gameMesh.UniqueMeshCount];

        foreach (var prefab in stockPrefabs.Concat(_prefabs))
        {
            if (!usedPrefabs.Used(prefab.Id) || (prefab.Id < RawGame.CurrentNumbStockPrefabs && StockBlocks.IsScriptPrefab(prefab.Id)))
            {
                continue;
            }

            var meshInfo = _gameMesh.GetBlockMesh(prefab.Id);

            for (int i = 0; i < meshInfo.MeshCount; i++)
            {
                var objectId = (FcObject)_objectIdCounter++;
                short objectInPrefabMeshIndex = (short)i;

                meshInfo.GetMesh(i, out var mesh, out int uniqueMeshIndex);

                if (uniqueMeshInfo[uniqueMeshIndex] is null)
                {
                    InitUnique(meshInfo, prefab, i);
                }

                var (mass, pos, sizeMin, sizeMax, foundPhysics, shape) = uniqueMeshInfo[uniqueMeshIndex]!.Value;
                pos += (Vector3)mesh.Position;

                var rigidBody = BulletCreate(pos, Quaternion.Identity, objectId);
                rigidBody.UpdateInertiaTensor();

                RuntimeObject rObject = new(objectId, uniqueMeshIndex, prefab.Id, objectInPrefabMeshIndex, rigidBody, pos, Quaternion.Identity, sizeMin, sizeMax, mass, prefab.Id == mainId, !foundPhysics);

                _objects.Add(rObject);
                _idToObject.Add(rObject.Id, rObject);

                rigidBody.SetCollisionShape(shape);
                if (foundPhysics)
                {
                    shape.CalculateLocalInertia(mass, out var localInertia);
                    rigidBody.SetMassProps(mass, localInertia);
                }

                if (rObject.IsVisible)
                {
                    _world.AddRigidBody(rObject.RigidBody);
                }

                if (foundPhysics)
                {
                    rObject.Unfix(_world);
                }
            }
        }

        InitConnectedObjects(usedPrefabs);

        void InitUnique(BlockMesh meshInfo, Prefab prefab, int meshIndex)
        {
            meshInfo.GetMesh(meshIndex, out var mesh, out int uniqueMeshIndex);

            var blocks = prefab.Blocks;
            ushort[] blocksArray = blocks.Array.Array;

            float totalVolume = 0f;
            Vector3 centerOfMass = Vector3.Zero;
            Vector3 sizeMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 sizeMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            bool foundPhysics = false;

            foreach (var block in mesh)
            {
                var meshPos = block.Offset;
                var blockPos = mesh.Position;

                int index = blocks.Index(blockPos);
                ushort blockId = blocksArray[index];

                if (blockId is 0)
                {
                    continue;
                }

                var currentSegment = _prefabs.GetSegmentOrStock(blockId);
                var currentPrefab = _prefabs.GetPrefabOrStock(currentSegment.PrefabId);
                var currentSegmentMesh = _gameMesh.GetSegmentMesh(blockId);

                if (currentPrefab.Type is PrefabType.Physics or PrefabType.Normal)
                {
                    bool foundMesh = false;

                    for (int segmentMeshIndex = 0; segmentMeshIndex < currentSegmentMesh.MeshCount; segmentMeshIndex++)
                    {
                        if (meshInfo.BlockMeshIds[segmentMeshIndex + meshInfo.BlockMeshIdOffsets[index]] != meshIndex)
                        {
                            continue;
                        }

                        if (currentPrefab.Type == PrefabType.Physics)
                        {
                            foundPhysics = true;

                            var currentMesh = currentSegmentMesh.Meshes[segmentMeshIndex];
                            var boundsMin = currentMesh.MinPos;
                            var boundsMax = currentMesh.MaxPos;
                            Vector3 size = (Vector3)(boundsMax - boundsMin + int3.One) * 0.125f;
                            float volume = size.X * size.Y * size.Z;
                            totalVolume += volume;

                            Vector3 worldBoundsMin = ((Vector3)boundsMin * 0.125f) + (Vector3)blockPos;
                            Vector3 worldBoundsMax = ((Vector3)boundsMax * 0.125f) + (Vector3)blockPos + new Vector3(0.125f);

                            centerOfMass += ((size * 0.5f) + worldBoundsMin) * volume;

                            sizeMin = Vector3.Min(sizeMin, worldBoundsMin);
                            sizeMax = Vector3.Max(sizeMax, worldBoundsMax);
                        }
                        else
                        {
                            if (foundMesh)
                            {
                                continue;
                            }

                            centerOfMass += (Vector3)blockPos + new Vector3(0.5f);
                            totalVolume++;

                            sizeMin = Vector3.Min(sizeMin, (Vector3)blockPos);
                            sizeMax = Vector3.Max(sizeMax, (Vector3)blockPos + Vector3.One);

                            foundMesh = true;
                        }
                    }
                }
            }

            var compoundShape = new CompoundShape(true, 0);
            Vector3 shapePos = centerOfMass * 1f / ((totalVolume == 0.0f) ? 1.0f : totalVolume);
            sizeMin -= shapePos;
            sizeMax -= shapePos;

            foreach (var block in mesh)
            {
                var meshPos = block.Offset;
                var blockPos = mesh.Position;

                int index = blocks.Index(blockPos);
                ushort blockId = blocksArray[index];

                if (blockId is 0)
                {
                    continue;
                }

                var currentSegment = _prefabs.GetSegmentOrStock(blockId);
                var currentPrefab = _prefabs.GetPrefabOrStock(currentSegment.PrefabId);
                var currentSegmentMesh = _gameMesh.GetSegmentMesh(blockId);

                if (currentPrefab.Collider == PrefabCollider.None || currentSegmentMesh.MeshCount == 0)
                {
                    continue;
                }

                for (int segmentMeshIndex = 0; segmentMeshIndex < currentSegmentMesh.MeshCount; segmentMeshIndex++)
                {
                    if (meshInfo.BlockMeshIds[segmentMeshIndex + meshInfo.BlockMeshIdOffsets[index]] != meshIndex)
                    {
                        continue;
                    }

                    var currentMesh = currentSegmentMesh.Meshes[segmentMeshIndex];
                    var boundsMin = currentMesh.MinPos;
                    var boundsMax = currentMesh.MaxPos;

                    Vector3 size = (Vector3)(boundsMax - boundsMin + int3.One) * 0.125f;

                    Vector3 offset = (size * 0.5f) + ((Vector3)boundsMin * 0.125f) + (Vector3)blockPos - shapePos;

                    //uint connectsToSideBitfield = 0;
                    int colliderType;
                    switch (currentPrefab.Collider)
                    {
                        case PrefabCollider.Sphere:
                            {
                                colliderType = 2;
                                size = new Vector3(MathF.Max(MathF.Max(size.X, size.Y), size.Z));
#pragma warning disable IDE0059 // Unnecessary assignment of a value
                                //connectsToSideBitfield = 0;
#pragma warning restore IDE0059 // Unnecessary assignment of a value
                            }

                            break;
                        case PrefabCollider.Box:
                            {
                                // todo: stretch the box if possible
                                /*for (int sideIndex = 0; sideIndex < 6; sideIndex++)
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
                                            Debug.Assert(sideIndex == 5, $"{nameof(sideIndex)} should be in the range 0-5.");
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
                                                //var (neighborBoundsMin, neighborBoundsMax) = GetMeshBounds(_prefabs.GetSegmentOrStock(neighborId).Voxels, neighborSegmentMeshes, (byte)neighborMeshIndex);
                                                var neighborMesh = neighborSegmentMeshes.Meshes[neighborMeshIndex];
                                                var neighborBoundsMin = neighborMesh.MinPos;
                                                var neighborBoundsMax = neighborMesh.MaxPos;

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
                                            //if (currentSegmentMeshes.Meshes[meshIndex].GetSideBitfield(sideIndex) == neighborSegmentMeshes.Meshes////[neighborMeshIndexToUse].GetSideBitfield(sideIndex ^ 1))
                                            //{
                                            //    connectsToSideBitfield |= (uint)(1L << (sideIndex & 0b111111));
                                            //}
                                        }
                                    }
                                }
    */
                                colliderType = 1;
                            }

                            break;
                        default:
                            {
                                Debug.Assert(currentPrefab.Collider == PrefabCollider.None, $"{nameof(currentPrefab)}.{nameof(currentPrefab.Collider)} should be valid.");
                                colliderType = 3;
#pragma warning disable IDE0059 // Unnecessary assignment of a value
                                //connectsToSideBitfield = 0;
#pragma warning restore IDE0059 // Unnecessary assignment of a value
                            }

                            break;
                    }

                    if (!_collisionShapeCache.TryGetValue((colliderType, size), out var shape))
                    {
                        switch (colliderType)
                        {
                            case 1:
                                shape = new BoxShape(size * 0.5f);
                                break;
                            case 2:
                                shape = new SphereShape(size.X * 0.5f);
                                break;
                            default:
                                continue;
                        }

                        _collisionShapeCache.Add((colliderType, size), shape);
                    }

                    var trans = Transform.Identity;
                    trans.Translation = offset;

                    compoundShape.AddChildShape(in trans, shape);
                }
            }

            uniqueMeshInfo[uniqueMeshIndex] = (totalVolume, shapePos, sizeMin, sizeMax, foundPhysics, compoundShape);
        }
    }

    private void InitConnectedObjects(PrefabUsedCache usedPrefabs)
    {
        var stockPrefabs = StockBlocks.PrefabList;

        var terminalInfos = PrefabTerminalInfo.Create(stockPrefabs.Concat(_prefabs), id =>
        {
            if (id < RawGame.CurrentNumbStockPrefabs)
            {
                return stockPrefabs.TryGetSegment(id, out var segment) && stockPrefabs.TryGetPrefab(segment.PrefabId, out var prefab) ? prefab : null;
            }
            else
            {
                return _prefabs.TryGetSegment(id, out var segment) && _prefabs.TryGetPrefab(segment.PrefabId, out var prefab) ? prefab : null;
            }
        });

        foreach (var prefab in stockPrefabs.Concat(_prefabs))
        {
            if (!usedPrefabs.Used(prefab.Id) ||
                (prefab.Id < RawGame.CurrentNumbStockPrefabs && StockBlocks.IsScriptPrefab(prefab.Id)) ||
                prefab.Connections.Count == 0)
            {
                continue;
            }

            var meshInfo = _gameMesh.GetBlockMesh(prefab.Id);

            foreach (var connection in prefab.Connections)
            {
                AddConnection(prefab, meshInfo, connection);
            }
        }

        void AddConnection(Prefab prefab, BlockMesh meshInfo, Connection connection)
        {
            if (connection.IsFromOutside)
            {
                if (terminalInfos[prefab.Id].InputTerminals.Any(terminal => terminal.Position == connection.FromVoxel))
                {
                    return; // normal connection to outside
                }

                // self connection
                for (int i = _runner.EnvironmentCount - 1; i >= 0; i--)
                {
                    var env = _runner.GetEnvironment(i);
                    if (env.PrefabId == prefab.Id)
                    {
                        var outerPrefab = _prefabs.GetPrefabOrStock(_runner.GetEnvironment(env.OuterEnvironmentIndex).PrefabId);

                        AddConnection(outerPrefab, _gameMesh.GetBlockMesh(outerPrefab.Id), new Connection(env.OuterPosition, default, connection.FromVoxel, default));
                    }
                }

                return;
            }

            ushort blockId = prefab.Blocks.GetBlockOrDefault(connection.From);
            ushort segmentId = prefab.Blocks.GetBlockOrDefault(connection.From + (connection.FromVoxel / 8));

            if (blockId == 0 || segmentId == 0)
            {
                return;
            }

            var terminalInfo = terminalInfos[blockId];
            var segmentMeshes = _gameMesh.GetSegmentMesh(segmentId);

            if (!terminalInfo.OutputTerminals.Any(terminal => terminal.Position == connection.FromVoxel))
            {
                int localMeshIndex = segmentMeshes.VoxelMeshIndex[Voxels.Index(connection.FromVoxel % 8, 0)];
                if (localMeshIndex == 255)
                {
                    // connected to empty voxel
                    return;
                }

                int meshIndex = meshInfo.BlockMeshIds[localMeshIndex + meshInfo.BlockMeshIdOffsets[((int3)connection.From).ToIndex(meshInfo.Size.X, meshInfo.Size.Y)]];

                var obj = _objects.FirstOrDefault(obj => obj.OutsidePrefabId == prefab.Id && obj.InPrefabMeshIndex == meshIndex);

                if (obj is not null)
                {
#if RELEASE
                        _connectorToObject[(prefab.Id, connection.From, (byte3)connection.FromVoxel)] = obj.Id;
#else
                    if (!_connectorToObject.TryAdd((prefab.Id, connection.From, connection.FromVoxel), obj.Id))
                    {
                        Debug.Assert(_connectorToObject[(prefab.Id, connection.From, connection.FromVoxel)] == obj.Id, "If a connector has already been added, it should be the same one that was to be added.");
                    }
#endif
                }
            }
        }
    }

    private bool TryGetObject(FcObject @object, [NotNullWhen(true)] out RuntimeObject? rObject)
    {
        if (@object == FcObject.Null)
        {
            rObject = null;
            return false;
        }

        return _idToObject.TryGetValue(@object, out rObject);
    }

    private bool TryGetConstraint(FcConstraint constraint, [NotNullWhen(true)] out Generic6DofSpring2Constraint? bConstraint)
    {
        if (constraint == FcConstraint.Null)
        {
            bConstraint = null;
            return false;
        }

        return _idToConstraint.TryGetValue(constraint, out bConstraint);
    }

    private bool TryGetObjectByPos(ushort prefabId, int3 pos, int3 voxelPos, [NotNullWhen(true)] out RuntimeObject? rObject)
    {
        Prefab prefab;
        try
        {
            prefab = _prefabs.GetPrefabOrStock(prefabId);
        }
        catch
        {
            rObject = null;
            return false;
        }

        ushort segmentId = prefab.Blocks.GetBlockOrDefault(pos + (voxelPos / 8));

        if (segmentId == 0)
        {
            rObject = null;
            return false;
        }

        var meshInfo = _gameMesh.GetBlockMesh(prefab.Id);
        var segmentMeshes = _gameMesh.GetSegmentMesh(segmentId);

        int meshIndex = meshInfo.GetMeshAtPos(pos, segmentMeshes.VoxelMeshIndex[Voxels.Index(voxelPos % 8, 0)]);

        var obj = _objects.FirstOrDefault(obj => obj.OutsidePrefabId == prefab.Id && obj.InPrefabMeshIndex == meshIndex);

        if (obj is not null)
        {
            rObject = obj;
            return true;
        }

        rObject = null;
        return false;
    }

    private bool TryGetObjectByPos(ushort prefabId, int3 pos, int meshIndex, [MaybeNullWhen(false)] out RuntimeObject rObject)
    {
        Prefab prefab;
        try
        {
            prefab = _prefabs.GetPrefabOrStock(prefabId);
        }
        catch
        {
            rObject = null;
            return false;
        }

        var meshInfo = _gameMesh.GetBlockMesh(prefab.Id);

        if (meshInfo.BlockMeshIdOffsets.IsEmpty)
        {
            rObject = null;
            return false;
        }

        int meshOffset = meshInfo.GetMeshOffsetOrZero(pos);
        if (meshOffset == -1 || meshOffset >= meshInfo.BlockMeshIds.Length)
        {
            rObject = null;
            return false;
        }

        int prefabMeshIndex = meshInfo.BlockMeshIds[meshOffset + meshIndex];

        var obj = _objects.FirstOrDefault(obj => obj.OutsidePrefabId == prefab.Id && obj.InPrefabMeshIndex == prefabMeshIndex);

        if (obj is not null)
        {
            rObject = obj;
            return true;
        }

        rObject = null;
        return false;
    }
}
