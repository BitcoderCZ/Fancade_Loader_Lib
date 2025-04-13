using FancadeLoaderLib.Editing.Scripting.Settings;
using MathUtils.Vectors;
using System.Collections.Frozen;

namespace FancadeLoaderLib.Runtime;

public abstract class RuntimeContext : IRuntimeContext
{
    protected FcRandom rng = new();

    public abstract long CurrentFrame { get; }

    public abstract bool TakingBoxArt { get; }

    public virtual void SetRandomSeed(float seed)
        => rng.SetSeed(seed);

    public virtual float GetRandomValue(float min, float max)
        => rng.NextSingle(min, max);

    public abstract void InspectValue(RuntimeValue value, SignalType type, ushort prefabId, ushort3 inspectBlockPosition);

    public abstract bool TryGetTouch(TouchState state, int fingerIndex, out float2 touchPos);

    public abstract bool TryGetSwipe(out float3 direction);

    public abstract bool GetButtonPressed(ButtonType type);

    public abstract float3 GetJoystickDirection(JoystickType type);

    public abstract bool TryGetCollision(int firstObject, out int secondObject, out float impulse, out float3 normal);

    public abstract (float3 WorldNear, float3 WorldFar) ScreenToWorld(float2 screenPos);

    public abstract float2 WorldToScreen(float3 worldPos);

    public abstract int CloneObject(int id);

    public abstract float3 GetObjectPosition(int id);
}
