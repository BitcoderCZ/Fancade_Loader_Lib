using BulletSharp;
using FancadeLoaderLib.Editing.Scripting.Settings;
using FancadeLoaderLib.Runtime.Exceptions;
using FancadeLoaderLib.Runtime.Utils;
using MathUtils.Vectors;
using System.Diagnostics;
using System.Numerics;

namespace FancadeLoaderLib.Runtime.Bullet;

public sealed partial class FcWorld
{
    private sealed class BulletRuntimeContext : IRuntimeContext
    {
        private readonly FcWorld _world;
        private readonly IRuntimeContextBase _baseCtx;

        private readonly FcRandom _rng = new();

        public BulletRuntimeContext(FcWorld world, IRuntimeContextBase baseCtx)
        {
            _world = world;
            _baseCtx = baseCtx;
        }

        public float2 ScreenSize => _baseCtx.ScreenSize;

        public float3 Accelerometer => _baseCtx.Accelerometer;

        public long CurrentFrame { get; set; }

        public bool TakingBoxArt => _baseCtx.TakingBoxArt;

        // **************************************** Game ****************************************
        public void Win(int delay)
            => _baseCtx.Win(delay);

        public void Lose(int delay)
            => _baseCtx.Lose(delay);

        public void SetScore(float? score, float? coins, Ranking ranking)
            => _baseCtx.SetScore(score, coins, ranking);

        public void SetCamera(float3? position, Quaternion? rotation, float? range, bool perspective)
            => _baseCtx.SetCamera(position, rotation, range, perspective);

        public void SetLight(float3? position, Quaternion? rotation)
            => _baseCtx.SetLight(position, rotation);

        public void MenuItem(VariableReference? variable, FcObject picture, string name, MaxBuyCount maxBuyCount, PriceIncrease priceIncrease)
            => _baseCtx.MenuItem(variable, picture, name, maxBuyCount, priceIncrease);

        // **************************************** Objects ****************************************
        public FcObject GetObject(int3 position, byte3 voxelPosition, ushort prefabId)
        {
            if (_world._connectorToObject.TryGetValue((prefabId, position, voxelPosition), out var obj))
            {
                return obj;
            }

            return FcObject.Null;
        }

        public (float3 Position, Quaternion Rotation) GetObjectPosition(FcObject @object)
        {
            if (!_world.TryGetObject(@object, out var rObject))
            {
                // TODO: return outside position
                return (default, Quaternion.Identity);
            }

            return (rObject.Pos, rObject.Rot);
        }

        public void SetPosition(FcObject @object, float3? position, Quaternion? rotation)
        {
            if (!_world.TryGetObject(@object, out var rObject))
            {
                return;
            }

            if (position is { } pos && pos.IsInfOrNaN())
            {
                throw new InvalidInputException("Set Position");
            }

            if (rotation is { } rot && rot.IsInfOrNaN())
            {
                throw new InvalidInputException("Set Position");
            }

            rObject.SetRotPos(position, rotation);
        }

        public (bool Hit, float3 HitPos, FcObject HitObj) Raycast(float3 from, float3 to)
            => throw new NotImplementedException();

        public (float3 Min, float3 Max) GetSize(FcObject @object)
        {
            if (_world.TryGetObject(@object, out var rObject))
            {
                return (rObject.SizeMin, rObject.SizeMax);
            }

            // TODO: get level size
            return (float3.Zero, float3.Zero);
        }

        public void SetVisible(FcObject @object, bool visible)
        {
            if (_world.TryGetObject(@object, out var rObject))
            {
                rObject.IsVisible = visible;
                // TODO: add/remove from world
            }
        }

        public FcObject CreateObject(FcObject original)
            => throw new NotImplementedException();

        public void DestroyObject(FcObject @object)
            => throw new NotImplementedException();

        // **************************************** Sound ****************************************
        public float PlaySound(float volume, float pitch, FcSound sound)
            => _baseCtx.PlaySound(volume, pitch, sound);

        public void StopSound(float channel)
            => _baseCtx.StopSound(channel);

        public void AdjustVolumePitch(float channel, float? volume, float? pitch)
            => _baseCtx.AdjustVolumePitch(channel, volume, pitch);

        // **************************************** Physics ****************************************
        public void AddForce(FcObject @object, float3? force, float3? applyAt, float3? torque)
        {
            if (!_world.TryGetObject(@object, out var rObject))
            {
                return;
            }

            rObject.Unfix();

            if (force is { } forceVal)
            {
                if (forceVal.IsInfOrNaN())
                {
                    throw new InvalidInputException("Add Force");
                }

                if (applyAt is { } applyAtVal)
                {
                    if (applyAtVal.IsInfOrNaN())
                    {
                        throw new InvalidInputException("Add Force");
                    }

                    rObject.RigidBody.ApplyForce(forceVal.ToNumerics(), applyAtVal.ToNumerics());
                    rObject.RigidBody.Activate();
                }
                else
                {
                    rObject.RigidBody.ApplyCentralForce(forceVal.ToNumerics());
                    rObject.RigidBody.Activate();
                }
            }

            if (torque is { } torqueVal)
            {
                if (torqueVal.IsInfOrNaN())
                {
                    throw new InvalidInputException("Add Force");
                }

                rObject.RigidBody.ApplyTorque(torqueVal.ToNumerics());
                rObject.RigidBody.Activate();
            }
        }

        public (float3 Velocity, float3 Spin) GetVelocity(FcObject @object)
        {
            if (!_world.TryGetObject(@object, out var rObject))
            {
                return (default, default);
            }

            return (rObject.RigidBody.LinearVelocity.ToFloat3(), rObject.RigidBody.AngularVelocity.ToFloat3() * (180f / MathF.PI));
        }

        public void SetVelocity(FcObject @object, float3? velocity, float3? spin)
        {
            if (!_world.TryGetObject(@object, out var rObject))
            {
                return;
            }

            rObject.Unfix();

            if (velocity is { } velocityVal)
            {
                if (velocityVal.IsInfOrNaN())
                {
                    throw new InvalidInputException("Set Velocity");
                }

                rObject.RigidBody.LinearVelocity = velocityVal.ToNumerics();
                rObject.RigidBody.Activate();
            }

            if (spin is { } spinVal)
            {
                if (spinVal.IsInfOrNaN())
                {
                    throw new InvalidInputException("Set Velocity");
                }

                rObject.RigidBody.AngularVelocity = spinVal.ToNumerics() * (MathF.PI / 180f);
                rObject.RigidBody.Activate();
            }
        }

        public void SetLocked(FcObject @object, float3? position, float3? rotation)
        {
            if (!_world.TryGetObject(@object, out var rObject))
            {
                return;
            }

            rObject.Unfix();

            if (position is { } positionVal)
            {
                if (positionVal.IsInfOrNaN())
                {
                    throw new InvalidInputException("Set Locked");
                }

                rObject.RigidBody.LinearFactor = positionVal.ToNumerics();
            }

            if (rotation is { } rotationVal)
            {
                if (rotationVal.IsInfOrNaN())
                {
                    throw new InvalidInputException("Set Locked");
                }

                rObject.RigidBody.AngularFactor = rotationVal.ToNumerics();
            }
        }

        public void SetMass(FcObject @object, float mass)
        {
            if (!_world.TryGetObject(@object, out var rObject))
            {
                return;
            }

            rObject.Unfix();

            if (float.IsNaN(mass) || float.IsInfinity(mass))
            {
                throw new InvalidInputException("Set Mass/Friction/Bounciness");
            }

            rObject.Mass = mass;
        }

        public void SetFriction(FcObject @object, float friction)
        {
            if (!_world.TryGetObject(@object, out var rObject))
            {
                return;
            }

            if (float.IsNaN(friction) || float.IsInfinity(friction))
            {
                throw new InvalidInputException("Set Mass/Friction/Bounciness");
            }

            rObject.RigidBody.Friction = friction;
        }

        public void SetBounciness(FcObject @object, float bounciness)
        {
            if (!_world.TryGetObject(@object, out var rObject))
            {
                return;
            }

            if (float.IsNaN(bounciness) || float.IsInfinity(bounciness))
            {
                throw new InvalidInputException("Set Mass/Friction/Bounciness");
            }

            rObject.RigidBody.Restitution = bounciness;
        }

        public void SetGravity(float3 gravity)
        {
            if (gravity.IsInfOrNaN())
            {
                throw new InvalidInputException("Set Gravity");
            }

            _world._world.Gravity = gravity.ToNumerics();
        }

        public FcConstraint AddConstraint(FcObject @base, FcObject part, float3? pivot)
        {
            if (@base == part || !_world.TryGetObject(@base, out var rBase) || !_world.TryGetObject(part, out var rPart))
            {
                return FcConstraint.Null;
            }

            if (pivot is not { } pivotVal)
            {
                pivotVal = rPart.Pos;
            }

            if (pivotVal.IsInfOrNaN())
            {
                throw new InvalidInputException("Add Constraint");
            }

            rPart.Unfix();

            bool inverted = Matrix4x4.Invert(rBase.RigidBody.WorldTransform, out var invBase);
            Debug.Assert(inverted);
            inverted = Matrix4x4.Invert(rPart.RigidBody.WorldTransform, out var invPart);
            Debug.Assert(inverted);

            Vector3 localPivotA = Vector3.Transform(pivotVal.ToNumerics(), invBase);
            Vector3 localPivotB = Vector3.Transform(pivotVal.ToNumerics(), invPart);

            var frameInA = Matrix4x4.CreateTranslation(localPivotA);
            var frameInB = Matrix4x4.CreateTranslation(localPivotB);

            Generic6DofSpring2Constraint constraint = new Generic6DofSpring2Constraint(rBase.RigidBody, rPart.RigidBody, frameInA, frameInB, RotateOrder.XYZ);

            constraint.AngularLowerLimit = Vector3.Zero;
            constraint.AngularUpperLimit = Vector3.Zero;

            _world._constraints.Add(constraint);

            var id = (FcConstraint)_world._constraintIdCounter++;

            _world._idToConstraint.Add(id, constraint);

            _world._world.AddConstraint(constraint, true);
            return id;
        }

        public void LinearLimits(FcConstraint constraint, float3 lower, float3 upper)
        {
            if (!_world.TryGetConstraint(constraint, out var bConstraint))
            {
                return;
            }

            bConstraint.LinearLowerLimit = lower.ToNumerics();
            bConstraint.LinearUpperLimit = upper.ToNumerics();

            bConstraint.RigidBodyB.Activate(true);
        }

        public void AngularLimits(FcConstraint constraint, float3 lower, float3 upper)
        {
            if (!_world.TryGetConstraint(constraint, out var bConstraint))
            {
                return;
            }

            bConstraint.AngularLowerLimit = lower.ToNumerics() * (MathF.PI / 180f);
            bConstraint.AngularUpperLimit = upper.ToNumerics() * (MathF.PI / 180f);

            bConstraint.RigidBodyB.Activate(true);
        }

        public void LinearSpring(FcConstraint constraint, float3 stiffness, float3 damping)
        {
            if (!_world.TryGetConstraint(constraint, out var bConstraint))
            {
                return;
            }

            for (int i = 0; i < 3; i++)
            {
                bConstraint.EnableSpring(i, stiffness[i] != 0f);
                bConstraint.SetStiffness(i, stiffness[i], true);
                bConstraint.SetDamping(i, damping[i], true);
            }

            bConstraint.RigidBodyB.Activate(true);
        }

        public void AngularSpring(FcConstraint constraint, float3 stiffness, float3 damping)
        {
            if (!_world.TryGetConstraint(constraint, out var bConstraint))
            {
                return;
            }

            for (int i = 0; i < 3; i++)
            {
                bConstraint.EnableSpring(i + 3, stiffness[i] != 0f);
                bConstraint.SetStiffness(i + 3, stiffness[i], true);
                bConstraint.SetDamping(i + 3, damping[i], true);
            }

            bConstraint.RigidBodyB.Activate(true);
        }

        public void LinearMotor(FcConstraint constraint, float3 speed, float3 force)
        {
            if (!_world.TryGetConstraint(constraint, out var bConstraint))
            {
                return;
            }

            for (int i = 0; i < 3; i++)
            {
                bConstraint.EnableMotor(i, force[i] != 0f);
                bConstraint.SetTargetVelocity(i, speed[i]);
                bConstraint.SetMaxMotorForce(i, force[i]);
            }

            bConstraint.RigidBodyA.Activate(true);
            bConstraint.RigidBodyB.Activate(true);
        }

        public void AngularMotor(FcConstraint constraint, float3 speed, float3 force)
        {
            if (!_world.TryGetConstraint(constraint, out var bConstraint))
            {
                return;
            }

            speed *= MathF.PI / 180f; // deg to rad

            for (int i = 0; i < 3; i++)
            {
                bConstraint.EnableMotor(i + 3, force[i] != 0f);
                bConstraint.SetTargetVelocity(i + 3, -speed[i]);
                bConstraint.SetMaxMotorForce(i + 3, force[i]);
            }

            bConstraint.RigidBodyA.Activate(true);
            bConstraint.RigidBodyB.Activate(true);
        }

        // **************************************** Control ****************************************
        public bool TryGetTouch(TouchState state, int fingerIndex, out float2 touchPos)
            => _baseCtx.TryGetTouch(state, fingerIndex, out touchPos);

        public bool TryGetSwipe(out float3 direction)
            => _baseCtx.TryGetSwipe(out direction);

        public bool GetButtonPressed(ButtonType type)
            => _baseCtx.GetButtonPressed(type);

        public float3 GetJoystickDirection(JoystickType type)
            => _baseCtx.GetJoystickDirection(type);

        public bool TryGetCollision(FcObject firstObject, out FcObject secondObject, out float impulse, out float3 normal)
            => throw new NotImplementedException();

        // **************************************** Math ****************************************
        public void SetRandomSeed(float seed)
            => _rng.SetSeed(seed);

        public float GetRandomValue(float min, float max)
            => _rng.NextSingle(min, max);

        public (float3 WorldNear, float3 WorldFar) ScreenToWorld(float2 screenPos)
            => _baseCtx.ScreenToWorld(screenPos);

        public float2 WorldToScreen(float3 worldPos)
            => _baseCtx.WorldToScreen(worldPos);

        // **************************************** Values ****************************************
        public void InspectValue(RuntimeValue value, SignalType type, string? variableName, ushort prefabId, ushort3 inspectBlockPosition)
            => _baseCtx.InspectValue(value, type, variableName, prefabId, inspectBlockPosition);
    }
}
