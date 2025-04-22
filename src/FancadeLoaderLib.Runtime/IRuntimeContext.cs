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

    void MenuItem(VariableReference? variable, int picture, string name, MaxBuyCount maxBuyCount, PriceIncrease priceIncrease);

    // **************************************** Objects ****************************************
    int GetObjectId(int3 position, byte3 voxelPosition);

    (float3 Position, Quaternion Rotation) GetObjectPosition(int objectId);

    void SetPosition(int objectId, float3? position, Quaternion? rotation);

    (bool Hit, float3 HitPos, int HitObjId) Raycast(float3 from, float3 to);

    (float3 Min, float3 Max) GetSize(int objectId);

    void SetVisible(int objectId, bool visible);

    int CreateObject(int original);

    void DestroyObject(int objectId);

    // **************************************** Sound ****************************************
    float PlaySound(float volume, float pitch, FcSound sound);

    void StopSound(float channel);

    void AdjustVolumePitch(float channel, float? volume, float? pitch);

    // **************************************** Control ****************************************
    bool TryGetTouch(TouchState state, int fingerIndex, out float2 touchPos);

    bool TryGetSwipe(out float3 direction);

    bool GetButtonPressed(ButtonType type);

    float3 GetJoystickDirection(JoystickType type);

    bool TryGetCollision(int firstObject, out int secondObject, out float impulse, out float3 normal);

    // **************************************** Math ****************************************
    void SetRandomSeed(float seed);

    float GetRandomValue(float min, float max);

    (float3 WorldNear, float3 WorldFar) ScreenToWorld(float2 screenPos);

    float2 WorldToScreen(float3 worldPos);

    // **************************************** Values ****************************************
    void InspectValue(RuntimeValue value, SignalType type, string? variableName, ushort prefabId, ushort3 inspectBlockPosition);
}
