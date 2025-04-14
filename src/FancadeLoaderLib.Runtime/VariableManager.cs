namespace FancadeLoaderLib.Runtime;

internal readonly struct VariableManager
{
    private readonly RuntimeValue[][] _variables = [];

    public VariableManager(int count)
    {
        _variables = new RuntimeValue[count][];

        for (int i = 0; i < _variables.Length; i++)
        {
            _variables[i] = [];
        }
    }

    public RuntimeValue GetVariableValue(int variableIndex, int index)
    {
        // TODO: bounds check variableId?
        var values = _variables[variableIndex];

        return index >= 0 && index < values.Length
            ? values[index]
            : RuntimeValue.Zero;
    }

    public void SetVariableValue(int variableIndex, int index, RuntimeValue value)
    {
        if (index < 0)
        {
            return;
        }

        // TODO: bounds check variableId?
        ref var values = ref _variables[variableIndex];

        if (index >= values.Length)
        {
            int newLen = index == 0 ? 1 : values.Length + 16;

            if (newLen < index + 1)
            {
                newLen = index + 1;
            }

            Array.Resize(ref values, newLen);
        }

        values[index] = value;
    }
}
