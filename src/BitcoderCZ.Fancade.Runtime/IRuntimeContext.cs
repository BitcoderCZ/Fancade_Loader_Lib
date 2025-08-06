using BitcoderCZ.Fancade.Editing.Scripting.Settings;
using BitcoderCZ.Maths.Vectors;
using System.Numerics;

namespace BitcoderCZ.Fancade.Runtime;

public interface IRuntimeContext
{
    float2 ScreenSize { get; }

    Vector3 Accelerometer { get; }

    long CurrentFrame { get; }

    bool TakingBoxArt { get; }

    // **************************************** Game ****************************************
    void Win(int delay);

    void Lose(int delay);

    void SetScore(float? score, float? coins, Ranking ranking);

    void SetCamera(Vector3? position, Quaternion? rotation, float? range, bool perspective);

    void SetLight(Vector3? position, Quaternion? rotation);

    void MenuItem(VariableReference? variable, FcObject picture, string name, MaxBuyCount maxBuyCount, PriceIncrease priceIncrease);

    // **************************************** Objects ****************************************
    FcObject GetObject(int3 position, byte3 voxelPosition, ushort prefabId);

    (Vector3 Position, Quaternion Rotation) GetObjectPosition(FcObject @object);

    void SetPosition(FcObject @object, Vector3? position, Quaternion? rotation);

    (bool Hit, Vector3 HitPos, FcObject HitObj) Raycast(Vector3 from, Vector3 to);

    (Vector3 Min, Vector3 Max) GetSize(FcObject @object);

    void SetVisible(FcObject @object, bool visible);

    FcObject CreateObject(FcObject original);

    void DestroyObject(FcObject @object);

    // **************************************** Sound ****************************************
    float PlaySound(float volume, float pitch, FcSound sound);

    void StopSound(float channel);

    void AdjustVolumePitch(float channel, float? volume, float? pitch);

    // **************************************** Physics ****************************************
    void AddForce(FcObject @object, Vector3? force, Vector3? applyAt, Vector3? torque);

    (Vector3 Velocity, Vector3 Spin) GetVelocity(FcObject @object);

    void SetVelocity(FcObject @object, Vector3? velocity, Vector3? spin);

    void SetLocked(FcObject @object, Vector3? position, Vector3? rotation);

    void SetMass(FcObject @object, float mass);

    void SetFriction(FcObject @object, float friction);

    void SetBounciness(FcObject @object, float bounciness);

    void SetGravity(Vector3 gravity);

    FcConstraint AddConstraint(FcObject @base, FcObject part, Vector3? pivot);

    void LinearLimits(FcConstraint constraint, Vector3? lower, Vector3? upper);

    void AngularLimits(FcConstraint constraint, Vector3? lower, Vector3? upper);

    void LinearSpring(FcConstraint constraint, Vector3? stiffness, Vector3? damping);

    void AngularSpring(FcConstraint constraint, Vector3? stiffness, Vector3? damping);

    void LinearMotor(FcConstraint constraint, Vector3? speed, Vector3? force);

    void AngularMotor(FcConstraint constraint, Vector3? speed, Vector3? force);

    // **************************************** Control ****************************************
    bool TryGetTouch(TouchState state, int fingerIndex, out float2 touchPos);

    bool TryGetSwipe(out Vector3 direction);

    bool GetButtonPressed(ButtonType type);

    Vector3 GetJoystickDirection(JoystickType type);

    bool TryGetCollision(FcObject firstObject, out FcObject secondObject, out float impulse, out Vector3 normal);

    // **************************************** Math ****************************************
    void SetRandomSeed(float seed);

    float GetRandomValue(float min, float max);

    (Vector3 WorldNear, Vector3 WorldFar) ScreenToWorld(float2 screenPos);

    float2 WorldToScreen(Vector3 worldPos);

    // **************************************** Values ****************************************
    void InspectValue(RuntimeValue value, SignalType type, string? variableName, ushort prefabId, ushort3 inspectBlockPosition);
}
