using FancadeLoaderLib.Editing.Scripting.Settings;
using MathUtils.Vectors;
using System.Numerics;

namespace FancadeLoaderLib.Runtime;

public interface IRuntimeContext
{
    float2 ScreenSize { get; }

    float3 Accelerometer { get; }

    long CurrentFrame { get; }

    bool TakingBoxArt { get; }

    // **************************************** Game ****************************************
    void Win(int delay);

    void Lose(int delay);

    void SetScore(float? score, float? coins, Ranking ranking);

    void SetCamera(float3? position, Quaternion? rotation, float? range, bool perspective);

    void SetLight(float3? position, Quaternion? rotation);

    void MenuItem(VariableReference? variable, FcObject picture, string name, MaxBuyCount maxBuyCount, PriceIncrease priceIncrease);

    // **************************************** Objects ****************************************
    FcObject GetObject(int3 position, byte3 voxelPosition, ushort prefabId);

    (float3 Position, Quaternion Rotation) GetObjectPosition(FcObject @object);

    void SetPosition(FcObject @object, float3? position, Quaternion? rotation);

    (bool Hit, float3 HitPos, FcObject HitObj) Raycast(float3 from, float3 to);

    (float3 Min, float3 Max) GetSize(FcObject @object);

    void SetVisible(FcObject @object, bool visible);

    FcObject CreateObject(FcObject original);

    void DestroyObject(FcObject @object);

    // **************************************** Sound ****************************************
    float PlaySound(float volume, float pitch, FcSound sound);

    void StopSound(float channel);

    void AdjustVolumePitch(float channel, float? volume, float? pitch);

    // **************************************** Physics ****************************************
    void AddForce(FcObject @object, float3? force, float3? applyAt, float3? torque);

    (float3 Velocity, float3 Spin) GetVelocity(FcObject @object);

    void SetVelocity(FcObject @object, float3? velocity, float3? spin);

    void SetLocked(FcObject @object, float3? position, float3? rotation);

    void SetMass(FcObject @object, float mass);

    void SetFriction(FcObject @object, float friction);

    void SetBounciness(FcObject @object, float bounciness);

    void SetGravity(float3 gravity);

    FcConstraint AddConstraint(FcObject @base, FcObject part, float3? pivot);

    void LinearLimits(FcConstraint constraint, float3? lower, float3? upper);

    void AngularLimits(FcConstraint constraint, float3? lower, float3? upper);

    void LinearSpring(FcConstraint constraint, float3? stiffness, float3? damping);

    void AngularSpring(FcConstraint constraint, float3? stiffness, float3? damping);

    void LinearMotor(FcConstraint constraint, float3? speed, float3? force);

    void AngularMotor(FcConstraint constraint, float3? speed, float3? force);

    // **************************************** Control ****************************************
    bool TryGetTouch(TouchState state, int fingerIndex, out float2 touchPos);

    bool TryGetSwipe(out float3 direction);

    bool GetButtonPressed(ButtonType type);

    float3 GetJoystickDirection(JoystickType type);

    bool TryGetCollision(FcObject firstObject, out FcObject secondObject, out float impulse, out float3 normal);

    // **************************************** Math ****************************************
    void SetRandomSeed(float seed);

    float GetRandomValue(float min, float max);

    (float3 WorldNear, float3 WorldFar) ScreenToWorld(float2 screenPos);

    float2 WorldToScreen(float3 worldPos);

    // **************************************** Values ****************************************
    void InspectValue(RuntimeValue value, SignalType type, string? variableName, ushort prefabId, ushort3 inspectBlockPosition);
}
