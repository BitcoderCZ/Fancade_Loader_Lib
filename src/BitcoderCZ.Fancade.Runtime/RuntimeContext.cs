using BitcoderCZ.Fancade;
using BitcoderCZ.Fancade.Editing.Scripting.Settings;
using MathUtils.Vectors;
using System.Numerics;

namespace BitcoderCZ.Fancade.Runtime;

public abstract class RuntimeContext : IRuntimeContext
{
    protected FcRandom rng = new();

    public abstract float2 ScreenSize { get; }

    public abstract float3 Accelerometer { get; }

    public abstract long CurrentFrame { get; }

    public abstract bool TakingBoxArt { get; }

    public virtual void SetRandomSeed(float seed)
        => rng.SetSeed(seed);

    public virtual float GetRandomValue(float min, float max)
        => rng.NextSingle(min, max);

    // **************************************** Game ****************************************
    public abstract void Win(int delay);

    public abstract void Lose(int delay);

    public abstract void SetScore(float? score, float? coins, Ranking ranking);

    public abstract void SetCamera(float3? position, Quaternion? rotation, float? range, bool perspective);

    public abstract void SetLight(float3? position, Quaternion? rotation);

    public abstract void MenuItem(VariableReference? variable, FcObject picture, string name, MaxBuyCount maxBuyCount, PriceIncrease priceIncrease);

    // **************************************** Objects ****************************************
    public abstract FcObject GetObject(int3 position, byte3 voxelPosition, ushort prefabId);

    public abstract (float3 Position, Quaternion Rotation) GetObjectPosition(FcObject @object);

    public abstract void SetPosition(FcObject @object, float3? position, Quaternion? rotation);

    public abstract (bool Hit, float3 HitPos, FcObject HitObj) Raycast(float3 from, float3 to);

    public abstract (float3 Min, float3 Max) GetSize(FcObject @object);

    public abstract void SetVisible(FcObject @object, bool visible);

    public abstract FcObject CreateObject(FcObject original);

    public abstract void DestroyObject(FcObject @object);

    // **************************************** Sound ****************************************
    public abstract float PlaySound(float volume, float pitch, FcSound sound);

    public abstract void StopSound(float channel);

    public abstract void AdjustVolumePitch(float channel, float? volume, float? pitch);

    // **************************************** Physics ****************************************
    public abstract void AddForce(FcObject @object, float3? force, float3? applyAt, float3? torque);

    public abstract (float3 Velocity, float3 Spin) GetVelocity(FcObject @object);

    public abstract void SetVelocity(FcObject @object, float3? velocity, float3? spin);

    public abstract void SetLocked(FcObject @object, float3? position, float3? rotation);

    public abstract void SetMass(FcObject @object, float mass);

    public abstract void SetFriction(FcObject @object, float friction);

    public abstract void SetBounciness(FcObject @object, float bounciness);

    public abstract void SetGravity(float3 gravity);

    public abstract FcConstraint AddConstraint(FcObject @base, FcObject part, float3? pivot);

    public abstract void LinearLimits(FcConstraint constraint, float3? lower, float3? upper);

    public abstract void AngularLimits(FcConstraint constraint, float3? lower, float3? upper);

    public abstract void LinearSpring(FcConstraint constraint, float3? stiffness, float3? damping);

    public abstract void AngularSpring(FcConstraint constraint, float3? stiffness, float3? damping);

    public abstract void LinearMotor(FcConstraint constraint, float3? speed, float3? force);

    public abstract void AngularMotor(FcConstraint constraint, float3? speed, float3? force);

    // **************************************** Control ****************************************
    public abstract bool TryGetTouch(TouchState state, int fingerIndex, out float2 touchPos);

    public abstract bool TryGetSwipe(out float3 direction);

    public abstract bool GetButtonPressed(ButtonType type);

    public abstract float3 GetJoystickDirection(JoystickType type);

    public abstract bool TryGetCollision(FcObject firstObject, out FcObject secondObject, out float impulse, out float3 normal);

    // **************************************** Math ****************************************
    public abstract (float3 WorldNear, float3 WorldFar) ScreenToWorld(float2 screenPos);

    public abstract float2 WorldToScreen(float3 worldPos);

    // **************************************** Values ****************************************
    public abstract void InspectValue(RuntimeValue value, SignalType type, string? variableName, ushort prefabId, ushort3 inspectBlockPosition);
}
