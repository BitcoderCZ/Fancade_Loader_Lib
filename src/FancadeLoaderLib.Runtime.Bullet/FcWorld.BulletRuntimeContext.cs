using FancadeLoaderLib.Editing.Scripting.Settings;
using FancadeLoaderLib.Runtime.Exceptions;
using FancadeLoaderLib.Runtime.Utils;
using MathUtils.Vectors;
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
            if (@object == FcObject.Null || !_world._idToObject.TryGetValue(@object, out var rObject))
            {
                // TODO: return outside position
                return (default, Quaternion.Identity);
            }

            return (rObject.Pos, rObject.Rot);
        }

        public void SetPosition(FcObject @object, float3? position, Quaternion? rotation)
            => throw new NotImplementedException();

        public (bool Hit, float3 HitPos, FcObject HitObj) Raycast(float3 from, float3 to)
            => throw new NotImplementedException();

        public (float3 Min, float3 Max) GetSize(FcObject @object)
        {
            var rObject = _world.GetObject(@object);

            if (rObject is not null)
            {
                return (rObject.SizeMin, rObject.SizeMax);
            }

            // TODO: get level size
            return (float3.Zero, float3.Zero);
        }

        public void SetVisible(FcObject @object, bool visible)
        {
            var rObject = _world.GetObject(@object);

            if (rObject is not null)
            {
                rObject.IsVisible = visible;
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
            => throw new NotImplementedException();

        public (float3 Velocity, float3 Spin) GetVelocity(FcObject @object)
            => throw new NotImplementedException();

        public void SetVelocity(FcObject @object, float3? velocity, float3? spin)
            => throw new NotImplementedException();

        public void SetLocked(FcObject @object, float3? position, float3? rotation)
            => throw new NotImplementedException();

        public void SetMass(FcObject @object, float mass)
            => throw new NotImplementedException();

        public void SetFriction(FcObject @object, float friction)
            => throw new NotImplementedException();

        public void SetBounciness(FcObject @object, float bounciness)
            => throw new NotImplementedException();

        public void SetGravity(float3 gravity)
        {
            if (gravity.IsInfOrNaN())
            {
                throw new InvalidInputException("Set Gravity");
            }

            _world._world.Gravity = gravity.ToNumerics();
        }

        public FcConstraint AddConstraint(FcObject @base, FcObject part, float3? pivot)
            => throw new NotImplementedException();

        public void LinearLimits(FcConstraint constraint, float3? lower, float3? upper)
            => throw new NotImplementedException();

        public void AngularLimits(FcConstraint constraint, float3? lower, float3? upper)
            => throw new NotImplementedException();

        public void LinearSpring(FcConstraint constraint, float3? stiffness, float3? damping)
            => throw new NotImplementedException();

        public void AngularSpring(FcConstraint constraint, float3? stiffness, float3? damping)
            => throw new NotImplementedException();

        public void LinearMotor(FcConstraint constraint, float3? speed, float3? force)
            => throw new NotImplementedException();

        public void AngularMotor(FcConstraint constraint, float3? speed, float3? force)
            => throw new NotImplementedException();

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
