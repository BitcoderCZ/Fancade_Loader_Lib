namespace BitcoderCZ.Fancade.Runtime;

public interface IVariableAccessor
{
    RuntimeValue GetVariableValue(int variableId, int index);

    void SetVariableValue(int variableId, int index, RuntimeValue value);
}
