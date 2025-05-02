using BulletSharp;
using System.Numerics;

namespace FancadeLoaderLib.Runtime.Bullet.Utils;

internal static class DynamicsWorldUtils
{
    public static RigidBody CreateBody(this DynamicsWorld world, Matrix4x4 startTransform, CollisionShape shape, float mass)
    {
        if (mass == 0)
        {
            return world.CreateStaticBody(startTransform, shape);
        }

        // Using a motion state is recommended,
        // it provides interpolation capabilities and only synchronizes "active" objects
        var myMotionState = new DefaultMotionState(startTransform);

        Vector3 localInertia = shape.CalculateLocalInertia(mass);

        RigidBody body;
        using (var rbInfo = new RigidBodyConstructionInfo(mass, myMotionState, shape, localInertia))
        {
            body = new RigidBody(rbInfo);
        }

        world.AddRigidBody(body);

        return body;
    }

    public static RigidBody CreateStaticBody(this DynamicsWorld world, Matrix4x4 startTransform, CollisionShape shape)
    {
        const float StaticMass = 0;

        RigidBody body;
        using (var rbInfo = new RigidBodyConstructionInfo(StaticMass, null, shape)
        {
            StartWorldTransform = startTransform
        })
        {
            body = new RigidBody(rbInfo);
        }

        world.AddRigidBody(body);

        return body;
    }
}
