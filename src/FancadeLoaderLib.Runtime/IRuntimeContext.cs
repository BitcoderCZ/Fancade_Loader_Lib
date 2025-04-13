using FancadeLoaderLib.Editing.Scripting.Settings;
using MathUtils.Vectors;

namespace FancadeLoaderLib.Runtime;

public interface IRuntimeContext
{
    long CurrentFrame { get; }

    bool TakingBoxArt { get; }

    void Init(IEnumerable<Variable> variables);

    int GetVariableId(Variable variable);

    RuntimeValue GetVariableValue(int variableId, int index);

    void SetVariableValue(int variableId, int index, RuntimeValue value);

    void InspectValue(TerminalOutput output, SignalType type, ushort3 inspectBlockPosition);

    bool TryGetTouch(TouchState state, int fingerIndex,  out float2 touchPos);

    bool TryGetSwipe(out float3 direction);

    void SetRandomSeed(float seed);

    float GetRandomValue(float min, float max);

    (float3 WorldNear, float3 WorldFar) ScreenToWorld(float2 screenPos);

    float2 WorldToScreen(float3 worldPos);

    float3 GetObjectPosition(int id);

    int CloneObject(int id);
}
