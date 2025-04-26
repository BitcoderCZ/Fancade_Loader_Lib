using BulletSharp;
using BulletSharp.SoftBody;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace FancadeLoaderLib.Runtime.Bullet;

public sealed class FcWorld : IDisposable
{
    private readonly DiscreteDynamicsWorld _world;

    private int _disposed;

    private FcWorld()
    {
        var collisionConf = new DefaultCollisionConfiguration();
        var dispatcher = new CollisionDispatcher(collisionConf);
        var broadphase = new DbvtBroadphase();
        var solver = new SequentialImpulseConstraintSolver();
        _world = new DiscreteDynamicsWorld(dispatcher, broadphase, solver, collisionConf)
        {
            Gravity = new Vector3(0, -10, 0)
        };

        var groundShape = new StaticPlaneShape(new Vector3(0, 1, 0), 1);
        var groundMotionState = new DefaultMotionState(Matrix4x4.CreateTranslation(0, -1, 0));
        var groundRigidBodyCI = new RigidBodyConstructionInfo(0, groundMotionState, groundShape);
        var groundBody = new RigidBody(groundRigidBodyCI);
        groundBody.Restitution = 1f;
        _world.AddRigidBody(groundBody);

        var sphereShape = new SphereShape(1);
        float mass = 1.0f;
        var localInertia = sphereShape.CalculateLocalInertia(mass);
        var motionState = new DefaultMotionState(Matrix4x4.CreateTranslation(0, 10, 0));
        var bodyCI = new RigidBodyConstructionInfo(mass, motionState, sphereShape, localInertia);
        var body = new RigidBody(bodyCI);
        body.Restitution = 0.5f;
        _world.AddRigidBody(body);

        for (int i = 0; i < 300; i++)
        {
            _world.StepSimulation(1 / 60f);
            var trans = body.MotionState.WorldTransform;
            Console.WriteLine($"Step {i}: Sphere Y = {trans.Translation.Y}");
        }

        body.Dispose();
        motionState.Dispose();
        sphereShape.Dispose();
    }

    public static FcWorld Create(Func<IRuntimeContext, IAstRunner> runnerFactory)
    {
        return new();
    }

    public void RunFrame()
    {

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

        if (disposing)
        {
            _world.Dispose();
        }

        GC.SuppressFinalize(this);
    }
}
