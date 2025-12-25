// <copyright file="DynamicsWorldUtils.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.BulletSharp;
using BitcoderCZ.BulletSharp.Collision.CollisionDispatch;
using BitcoderCZ.BulletSharp.Dynamics;
using BitcoderCZ.BulletSharp.LinearMath;
using System.Numerics;

namespace BitcoderCZ.Fancade.Runtime.Simulated.Bullet.Utils;

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
        var myMotionState = new DefaultMotionState(startTransform, Matrix4x4.Identity);

        shape.CalculateLocalInertia(mass, out var localInertia);

        RigidBody body;
        var rbInfo = new RigidBodyConstructionInfo(mass, myMotionState, shape, localInertia);
        body = new RigidBody(in rbInfo);

        world.AddRigidBody(body);

        return body;
    }

    public static RigidBody CreateStaticBody(this DynamicsWorld world, Matrix4x4 startTransform, CollisionShape shape)
    {
        const float StaticMass = 0;

        RigidBody body;
        var rbInfo = new RigidBodyConstructionInfo(StaticMass, null, shape)
        {
            StartWorldTransform = startTransform,
        };
        body = new RigidBody(in rbInfo);

        world.AddRigidBody(body);

        return body;
    }
}
