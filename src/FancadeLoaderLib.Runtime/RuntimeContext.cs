using FancadeLoaderLib.Editing.Scripting.Settings;
using MathUtils.Vectors;
using System.Numerics;

namespace FancadeLoaderLib.Runtime;

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

    public abstract void MenuItem(VariableReference? variable, int picture, string name, MaxBuyCount maxBuyCount, PriceIncrease priceIncrease);

    // **************************************** Objects ****************************************
    public abstract int GetObjectId(int3 position, byte3 voxelPosition);

    public abstract (float3 Position, Quaternion Rotation) GetObjectPosition(int objectId);

    public abstract void SetPosition(int objectId, float3? position, Quaternion? rotation);

    public abstract (bool Hit, float3 HitPos, int HitObjId) Raycast(float3 from, float3 to);

    public abstract (float3 Min, float3 Max) GetSize(int objectId);

    public abstract void SetVisible(int objectId, bool visible);

    public abstract int CreateObject(int original);

    public abstract void DestroyObject(int objectId);

    // **************************************** Sound ****************************************
    public abstract float PlaySound(float volume, float pitch, FcSound sound);

    public abstract void StopSound(float channel);

    public abstract void AdjustVolumePitch(float channel, float volume, float pitch);

    // **************************************** Control ****************************************
    public abstract bool TryGetTouch(TouchState state, int fingerIndex, out float2 touchPos);

    public abstract bool TryGetSwipe(out float3 direction);

    public abstract bool GetButtonPressed(ButtonType type);

    public abstract float3 GetJoystickDirection(JoystickType type);

    public abstract bool TryGetCollision(int firstObject, out int secondObject, out float impulse, out float3 normal);

    // **************************************** Math ****************************************
    public abstract (float3 WorldNear, float3 WorldFar) ScreenToWorld(float2 screenPos);

    public abstract float2 WorldToScreen(float3 worldPos);

    // **************************************** Values ****************************************
    public abstract void InspectValue(TerminalOutput output, SignalType type, ushort prefabId, ushort3 inspectBlockPosition);
}
