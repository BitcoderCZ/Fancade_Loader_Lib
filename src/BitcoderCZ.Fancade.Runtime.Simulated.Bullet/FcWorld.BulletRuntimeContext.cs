using BitcoderCZ.BulletSharp;
using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Fancade.Editing.Scripting.Settings;
using BitcoderCZ.Fancade.Runtime.Exceptions;
using BitcoderCZ.Fancade.Runtime.Utils;
using BitcoderCZ.Maths.Vectors;
using System.Diagnostics;
using System.Numerics;

namespace BitcoderCZ.Fancade.Runtime.Simulated.Bullet;

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

        public Vector2 ScreenSize => _baseCtx.ScreenSize;

        public Vector3 Accelerometer => _baseCtx.Accelerometer;

        public long CurrentFrame { get; set; }

        public bool TakingBoxArt => _baseCtx.TakingBoxArt;

        public void Reset()
            => _rng.ResetSeed();

        // **************************************** Game ****************************************
        public void Win(int delay)
            => _baseCtx.Win(delay);

        public void Lose(int delay)
            => _baseCtx.Lose(delay);

        public void SetScore(float? score, float? coins, Ranking ranking)
            => _baseCtx.SetScore(score, coins, ranking);

        public void SetCamera(Vector3? position, Quaternion? rotation, float? range, bool perspective)
            => _baseCtx.SetCamera(position, rotation, range, perspective);

        public void SetLight(Vector3? position, Quaternion? rotation)
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
            else if (_world.TryGetObjectByPos(prefabId, position, voxelPosition, out var rObject))
            {
                return rObject.Id;
            }

            return FcObject.Null;
        }

        public (Vector3 Position, Quaternion Rotation) GetObjectPosition(FcObject @object, IFcEnvironment environment, int3 blockPosition)
        {
            if (@object == FcObject.Null)
            {
                if (environment.OuterEnvironmentIndex == -1)
                {
                    return ((Vector3)blockPosition + new Vector3(0.9375f, 0.1875f, 0.9375f), Quaternion.Identity);
                }

                // get the second top most environment
                while (true)
                {
                    var outer = _world._runner.GetEnvironment(environment.OuterEnvironmentIndex);

                    if (outer.OuterEnvironmentIndex == -1)
                    {
                        break;
                    }

                    environment = outer;
                }

                var (min, max) = _world._gameMesh.GetPrefabMeshBounds(environment.PrefabId);

                var segment = _world._prefabs.GetSegmentOrStock(_world._prefabs.GetPrefab(_world._mainPrefab).Blocks.GetBlockOrDefault(environment.OuterPosition));

                // prefab pos + size / 2
                Vector3 pos = (Vector3)(environment.OuterPosition - segment.PosInPrefab) + ((Vector3)((max - min) + int3.One) * 0.5f + (Vector3)min) * 0.125f;
                Quaternion rot = Quaternion.Identity;

                if (_world.TryGetObjectByPos(_world._mainPrefab, environment.OuterPosition, 0, out var obj))
                {
                    pos -= obj.Start.Position;
                    pos += obj.Pos;
                    rot = obj.Rot;
                }

                return (pos, rot);
            }

            if (!_world.TryGetObject(@object, out var rObject))
            {
                return (default, Quaternion.Identity);
            }

            return (rObject.Pos, rObject.Rot);
        }

        public void SetPosition(FcObject @object, Vector3? position, Quaternion? rotation)
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

        public (bool Hit, Vector3 HitPos, FcObject HitObj) Raycast(Vector3 from, Vector3 to)
        {
            if ((to - from).LengthSquared() < 1.0000001e-06f)
            {
                return (false, Vector3.Zero, FcObject.Null);
            }

            using (var callback = new ClosestRayResultCallback(ref from, ref to))
            {
                _world._world.RayTest(from, to, callback);

                if (callback.CollisionObject is null)
                {
                    return (false, Vector3.Zero, FcObject.Null);
                }

                return (true, callback.HitPointWorld, (FcObject)callback.CollisionObject.UserIndex);
            }
        }

        public (Vector3 Min, Vector3 Max) GetSize(FcObject @object)
        {
            if (_world.TryGetObject(@object, out var rObject))
            {
                return (rObject.SizeMin, rObject.SizeMax);
            }

            // TODO: get level size
            return (Vector3.Zero, Vector3.Zero);
        }

        public void SetVisible(FcObject @object, bool visible)
        {
            if (!_world.TryGetObject(@object, out var rObject))
            {
                return;
            }

            if (rObject.IsVisible)
            {
                if (!visible)
                {
                    for (int i = 0; i < _world._constraints.Count; i++)
                    {
                        var con = _world._constraints[i];

                        if (con.RigidBodyA == rObject.RigidBody || con.RigidBodyB == rObject.RigidBody)
                        {
                            _world._constraints.RemoveAt(i);
                            Debug.Assert(con.Userobject is FcConstraint);
                            _world._idToConstraint.Remove((FcConstraint)con.Userobject);
                            _world._world.RemoveConstraint(con);
                            con.Dispose();
                            i--;
                        }
                    }

                    Debug.Assert(rObject.RigidBody.IsInWorld);
                    _world._world.RemoveRigidBody(rObject.RigidBody);
                }
            }
            else if (visible)
            {
                Debug.Assert(!rObject.RigidBody.IsInWorld);
                _world._world.AddRigidBody(rObject.RigidBody);
            }

            rObject.IsVisible = visible;
        }

        public FcObject CreateObject(FcObject original)
        {
            if (!_world.TryGetObject(original, out var rOriginal))
            {
                return FcObject.Null;
            }

            var newId = (FcObject)_world._objectIdCounter++;
            var newRigidBody = BulletCreate(rOriginal.Pos + Vector3.One, rOriginal.Rot, newId);

            newRigidBody.UpdateInertiaTensor();

            RuntimeObject newObject = rOriginal.Clone(newId, newRigidBody, true);
            newObject.RigidBody.CollisionShape = rOriginal.RigidBody.CollisionShape;

            if (!rOriginal.IsFixed)
            {
                newObject.Unfix(_world._world);
            }

            _world._world.AddRigidBody(newRigidBody);
            _world._objects.Add(newObject);
            _world._idToObject.Add(newId, newObject);

            _world.OnObjectCreated?.Invoke(rOriginal, newObject);

            return newId;
        }

        public void DestroyObject(FcObject @object)
        {
            if (!_world.TryGetObject(@object, out var rObject) || !rObject.IsUserCreated)
            {
                return;
            }

            _world.OnObjectDestroyed?.Invoke(rObject);

            for (int i = 0; i < _world._constraints.Count; i++)
            {
                var con = _world._constraints[i];

                if (con.RigidBodyA == rObject.RigidBody || con.RigidBodyB == rObject.RigidBody)
                {
                    _world._constraints.RemoveAt(i);
                    Debug.Assert(con.Userobject is FcConstraint);
                    _world._idToConstraint.Remove((FcConstraint)con.Userobject);
                    _world._world.RemoveConstraint(con);
                    con.Dispose();
                    i--;
                }
            }

            rObject.RigidBody.MotionState?.Dispose();
            if (rObject.RigidBody.IsInWorld)
            {
                _world._world.RemoveRigidBody(rObject.RigidBody);
            }

            rObject.RigidBody.Dispose();

            _world._objects.Remove(rObject);
            _world._idToObject.Remove(rObject.Id);
        }

        // **************************************** Sound ****************************************
        public float PlaySound(float volume, float pitch, bool loop, FcSound sound)
            => _baseCtx.PlaySound(volume, pitch, loop, sound);

        public void StopSound(float channel)
            => _baseCtx.StopSound(channel);

        public void AdjustVolumePitch(float channel, float? volume, float? pitch)
            => _baseCtx.AdjustVolumePitch(channel, volume, pitch);

        // **************************************** Physics ****************************************
        public void AddForce(FcObject @object, Vector3? force, Vector3? applyAt, Vector3? torque)
        {
            if (!_world.TryGetObject(@object, out var rObject))
            {
                return;
            }

            rObject.Unfix(_world._world);

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

                    rObject.RigidBody.ApplyForce(forceVal, applyAtVal);
                    rObject.RigidBody.Activate();
                }
                else
                {
                    rObject.RigidBody.ApplyCentralForce(forceVal);
                    rObject.RigidBody.Activate();
                }
            }

            if (torque is { } torqueVal)
            {
                if (torqueVal.IsInfOrNaN())
                {
                    throw new InvalidInputException("Add Force");
                }

                rObject.RigidBody.ApplyTorque(torqueVal);
                rObject.RigidBody.Activate();
            }
        }

        public (Vector3 Velocity, Vector3 Spin) GetVelocity(FcObject @object)
        {
            if (!_world.TryGetObject(@object, out var rObject))
            {
                return (default, default);
            }

            return (rObject.RigidBody.LinearVelocity, rObject.RigidBody.AngularVelocity * (180f / MathF.PI));
        }

        public void SetVelocity(FcObject @object, Vector3? velocity, Vector3? spin)
        {
            if (!_world.TryGetObject(@object, out var rObject))
            {
                return;
            }

            rObject.Unfix(_world._world);

            if (velocity is { } velocityVal)
            {
                if (velocityVal.IsInfOrNaN())
                {
                    throw new InvalidInputException("Set Velocity");
                }

                rObject.RigidBody.LinearVelocity = velocityVal;
                rObject.RigidBody.Activate();
            }

            if (spin is { } spinVal)
            {
                if (spinVal.IsInfOrNaN())
                {
                    throw new InvalidInputException("Set Velocity");
                }

                rObject.RigidBody.AngularVelocity = spinVal * (MathF.PI / 180f);
                rObject.RigidBody.Activate();
            }
        }

        public void SetLocked(FcObject @object, Vector3? position, Vector3? rotation)
        {
            if (!_world.TryGetObject(@object, out var rObject))
            {
                return;
            }

            rObject.Unfix(_world._world);

            if (position is { } positionVal)
            {
                if (positionVal.IsInfOrNaN())
                {
                    throw new InvalidInputException("Set Locked");
                }

                rObject.RigidBody.LinearFactor = positionVal;
            }

            if (rotation is { } rotationVal)
            {
                if (rotationVal.IsInfOrNaN())
                {
                    throw new InvalidInputException("Set Locked");
                }

                rObject.RigidBody.AngularFactor = rotationVal;
            }
        }

        public void SetMass(FcObject @object, float mass)
        {
            if (!_world.TryGetObject(@object, out var rObject))
            {
                return;
            }

            rObject.Unfix(_world._world);

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

        public void SetGravity(Vector3 gravity)
        {
            if (gravity.IsInfOrNaN())
            {
                throw new InvalidInputException("Set Gravity");
            }

            _world._world.Gravity = gravity;
        }

        public FcConstraint AddConstraint(FcObject @base, FcObject part, Vector3? pivot)
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

            rPart.Unfix(_world._world);

            bool inverted = Matrix4x4.Invert(rBase.RigidBody.WorldTransform, out var invBase);
            Debug.Assert(inverted);
            inverted = Matrix4x4.Invert(rPart.RigidBody.WorldTransform, out var invPart);
            Debug.Assert(inverted);

            Vector3 localPivotA = Vector3.Transform(pivotVal, invBase);
            Vector3 localPivotB = Vector3.Transform(pivotVal, invPart);

            var frameInA = Matrix4x4.CreateTranslation(localPivotA);
            var frameInB = Matrix4x4.CreateTranslation(localPivotB);

            Generic6DofSpring2Constraint constraint = new Generic6DofSpring2Constraint(rBase.RigidBody, rPart.RigidBody, frameInA, frameInB, RotateOrder.XYZ);

            constraint.AngularLowerLimit = Vector3.Zero;
            constraint.AngularUpperLimit = Vector3.Zero;

            _world._constraints.Add(constraint);

            var id = (FcConstraint)_world._constraintIdCounter++;

            constraint.Userobject = id;
            _world._idToConstraint.Add(id, constraint);

            _world._world.AddConstraint(constraint, true);
            return id;
        }

        public void LinearLimits(FcConstraint constraint, Vector3 lower, Vector3 upper)
        {
            if (!_world.TryGetConstraint(constraint, out var bConstraint))
            {
                return;
            }

            bConstraint.LinearLowerLimit = lower;
            bConstraint.LinearUpperLimit = upper;

            bConstraint.RigidBodyB.Activate(true);
        }

        public void AngularLimits(FcConstraint constraint, Vector3 lower, Vector3 upper)
        {
            if (!_world.TryGetConstraint(constraint, out var bConstraint))
            {
                return;
            }

            bConstraint.AngularLowerLimit = lower * (MathF.PI / 180f);
            bConstraint.AngularUpperLimit = upper * (MathF.PI / 180f);

            bConstraint.RigidBodyB.Activate(true);
        }

        public void LinearSpring(FcConstraint constraint, Vector3 stiffness, Vector3 damping)
        {
            if (!_world.TryGetConstraint(constraint, out var bConstraint))
            {
                return;
            }

            for (int i = 0; i < 3; i++)
            {
                bConstraint.EnableSpring(i, stiffness.GetAxis(i) != 0f);
                bConstraint.SetStiffness(i, stiffness.GetAxis(i), true);
                bConstraint.SetDamping(i, damping.GetAxis(i), true);
            }

            bConstraint.RigidBodyB.Activate(true);
        }

        public void AngularSpring(FcConstraint constraint, Vector3 stiffness, Vector3 damping)
        {
            if (!_world.TryGetConstraint(constraint, out var bConstraint))
            {
                return;
            }

            for (int i = 0; i < 3; i++)
            {
                bConstraint.EnableSpring(i + 3, stiffness.GetAxis(i) != 0f);
                bConstraint.SetStiffness(i + 3, stiffness.GetAxis(i), true);
                bConstraint.SetDamping(i + 3, damping.GetAxis(i), true);
            }

            bConstraint.RigidBodyB.Activate(true);
        }

        public void LinearMotor(FcConstraint constraint, Vector3 speed, Vector3 force)
        {
            if (!_world.TryGetConstraint(constraint, out var bConstraint))
            {
                return;
            }

            for (int i = 0; i < 3; i++)
            {
                bConstraint.EnableMotor(i, force.GetAxis(i) != 0f);
                bConstraint.SetTargetVelocity(i, speed.GetAxis(i));
                bConstraint.SetMaxMotorForce(i, force.GetAxis(i));
            }

            bConstraint.RigidBodyA.Activate(true);
            bConstraint.RigidBodyB.Activate(true);
        }

        public void AngularMotor(FcConstraint constraint, Vector3 speed, Vector3 force)
        {
            if (!_world.TryGetConstraint(constraint, out var bConstraint))
            {
                return;
            }

            speed *= MathF.PI / 180f; // deg to rad

            for (int i = 0; i < 3; i++)
            {
                bConstraint.EnableMotor(i + 3, force.GetAxis(i) != 0f);
                bConstraint.SetTargetVelocity(i + 3, -speed.GetAxis(i));
                bConstraint.SetMaxMotorForce(i + 3, force.GetAxis(i));
            }

            bConstraint.RigidBodyA.Activate(true);
            bConstraint.RigidBodyB.Activate(true);
        }

        // **************************************** Control ****************************************
        public bool TryGetTouch(TouchState state, int fingerIndex, out Vector2 touchPos)
            => _baseCtx.TryGetTouch(state, fingerIndex, out touchPos);

        public bool TryGetSwipe(out Vector3 direction)
            => _baseCtx.TryGetSwipe(out direction);

        public bool GetButtonPressed(ButtonType type)
            => _baseCtx.GetButtonPressed(type);

        public Vector3 GetJoystickDirection(JoystickType type)
            => _baseCtx.GetJoystickDirection(type);

        public bool TryGetCollision(FcObject firstObject, out FcObject secondObject, out float impulse, out Vector3 normal)
        {
            if (!_world.TryGetObject(firstObject, out var rFirstObject) || rFirstObject.MaxForceCollision.Force == -1f)
            {
                secondObject = FcObject.Null;
                impulse = 0f;
                normal = Vector3.Zero;
                return false;
            }

            secondObject = rFirstObject.MaxForceCollision.OtherObject;
            impulse = rFirstObject.MaxForceCollision.Force;
            normal = rFirstObject.MaxForceCollision.Normal;

            return true;
        }

        // **************************************** Math ****************************************
        public void SetRandomSeed(float seed)
            => _rng.SetSeed(seed);

        public float GetRandomValue(float min, float max)
            => _rng.NextSingle(min, max);

        public (Vector3 WorldNear, Vector3 WorldFar) ScreenToWorld(Vector2 screenPos)
            => _baseCtx.ScreenToWorld(screenPos);

        public Vector2 WorldToScreen(Vector3 worldPos)
            => _baseCtx.WorldToScreen(worldPos);

        // **************************************** Values ****************************************
        public void InspectValue(RuntimeValue value, SignalType type, string? variableName, ushort prefabId, int3 inspectBlockPosition)
            => _baseCtx.InspectValue(value, type, variableName, prefabId, inspectBlockPosition);
    }
}
