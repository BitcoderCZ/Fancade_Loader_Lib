using BulletSharp;
using FancadeLoaderLib.Runtime.Bullet.Utils;
using System.Numerics;
using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib.Runtime.Bullet;

public sealed partial class FcWorld : IDisposable
{
    private readonly DiscreteDynamicsWorld _world;

    private readonly BulletRuntimeContext _runtimeCtx;

    private readonly IAstRunner _runner;

    private readonly RigidBody _groundPlane;

    private readonly List<RuntimeObject> _objects = [];

    private readonly Dictionary<FcObject, int> _idToIndex = [];

    private int _disposed;

    private FcWorld(IRuntimeContextBase runtimeContext, Func<IRuntimeContext, IAstRunner> runnerFactory)
    {
        _runtimeCtx = new BulletRuntimeContext(this, runtimeContext);
        _runner = runnerFactory(_runtimeCtx);

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
    }

    public static FcWorld Create(ushort prefabId, PrefabList prefabs, IRuntimeContextBase runtimeContext, Func<IRuntimeContext, IAstRunner> runnerFactory)
    {
        ThrowIfNull(runtimeContext, nameof(runtimeContext));
        ThrowIfNull(runnerFactory, nameof(runnerFactory));

        return new FcWorld(runtimeContext, runnerFactory);
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
