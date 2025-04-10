using MathUtils.Vectors;

namespace FancadeLoaderLib.Runtime;

public interface IRuntimeContext
{
    void Init(IEnumerable<Variable> variables);

    int GetVariableId(Variable variable);

    void SetVariableValue(int variableId, int index, RuntimeValue value);

    RuntimeValue GetVariableValue(int variableId, int index);

    void InspectValue(TerminalOutput output, SignalType type, ushort3 inspectBlockPosition);

    void SetRandomSeed(float seed);

    float GetRandomValue(float min, float max);

    float3 GetObjectPosition(int id);

    int CloneObject(int id);
}
