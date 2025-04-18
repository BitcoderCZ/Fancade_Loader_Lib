using System.Numerics;

namespace FancadeLoaderLib.Runtime;

internal readonly struct VariableManager
{
    private readonly (RuntimeValue[] Values, int Length)[] _variables = [];

    public VariableManager(int count)
    {
        _variables = new (RuntimeValue[], int)[count];

        for (int i = 0; i < _variables.Length; i++)
        {
            _variables[i] = ([], 0);
        }
    }

    public RuntimeValue GetVariableValue(int variableIndex, int index)
    {
        // TODO: bounds check variableId?
        var values = _variables[variableIndex].Values;

        return index >= 0 && index < values.Length
            ? values[index]
            : RuntimeValue.Zero;
    }

    public Span<RuntimeValue> GetVariableValues(int variableIndex)
    {
        // TODO: bounds check variableId?
        var item = _variables[variableIndex];

        return item.Values.AsSpan(0, item.Length);
    }

    public void SetVariableValue(int variableIndex, int index, RuntimeValue value)
    {
        if (index < 0)
        {
            return;
        }

        // TODO: bounds check variableId?
        ref var item = ref _variables[variableIndex];

        if (index >= item.Values.Length)
        {
            int newLen = index == 0 ? 1 : item.Values.Length + 16;

            if (newLen < index + 1)
            {
                newLen = index + 1;
            }

            int oldLength = item.Values.Length;

            Array.Resize(ref item.Values, newLen);

            // make sure that if this variable is of type rotation the values are valid, only the last float is set to 1, so doesn't affect any other type
            Array.Fill(item.Values, new RuntimeValue(Quaternion.Identity), oldLength, newLen - oldLength);
        }

        if (index >= item.Length)
        {
            item.Length = index + 1;
        }

        item.Values[index] = value;
    }
}
