using FancadeLoaderLib.Editing.Scripting.Settings;
using MathUtils.Vectors;
using System.Collections.Frozen;

namespace FancadeLoaderLib.Runtime;

public abstract class RuntimeContext : IRuntimeContext
{
    protected FrozenDictionary<Variable, int> variableToId = FrozenDictionary<Variable, int>.Empty;
    protected RuntimeValue[][] variables = [];

    protected FcRandom rng = new();

    public abstract long CurrentFrame { get; }

    public abstract bool TakingBoxArt { get; }

    public void Init(IEnumerable<Variable> variables)
    {
        int id = 0;

        variableToId = variables.ToFrozenDictionary(variable => variable, variable => id++);

        this.variables = new RuntimeValue[variableToId.Count][];

        for (int i = 0; i < this.variables.Length; i++)
        {
            this.variables[i] = [];
        }

        rng = new();
    }

    public int GetVariableId(Variable variable)
        => variableToId[variable];

    public RuntimeValue GetVariableValue(int variableId, int index)
    {
        var values = variables[variableId];

        return index >= 0 && index < values.Length
            ? values[index]
            : RuntimeValue.Zero;
    }

    public void SetVariableValue(int variableId, int index, RuntimeValue value)
    {
        if (index < 0)
        {
            return;
        }

        ref var values = ref variables[variableId];

        if (index >= values.Length)
        {
            int newLen = values.Length + 16;

            if (newLen < index + 1)
            {
                newLen = index + 1;
            }

            Array.Resize(ref values, newLen);
        }

        values[index] = value;
    }

    public void SetRandomSeed(float seed)
        => rng.SetSeed(seed);

    public float GetRandomValue(float min, float max)
        => rng.NextSingle(min, max);

    public abstract void InspectValue(TerminalOutput output, SignalType type, ushort3 inspectBlockPosition);

    public abstract bool TryGetTouch(TouchState state, int fingerIndex, out float2 touchPos);

    public abstract bool TryGetSwipe(out float3 direction);

    public abstract (float3 WorldNear, float3 WorldFar) ScreenToWorld(float2 screenPos);

    public abstract float2 WorldToScreen(float3 worldPos);

    public abstract int CloneObject(int id);

    public abstract float3 GetObjectPosition(int id);
}
