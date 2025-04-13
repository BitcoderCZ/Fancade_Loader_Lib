using System.Runtime.InteropServices;

namespace FancadeLoaderLib.Runtime;

[StructLayout(LayoutKind.Sequential, Size = 8)]
public readonly struct VariableReference
{
    public readonly int VariableId;
    public readonly int Index;

    public VariableReference(int variableId, int index)
    {
        VariableId = variableId;
        Index = index;
    }

    public readonly RuntimeValue GetValue(IVariableAccessor variableAccessor)
        => variableAccessor.GetVariableValue(VariableId, Index);
}
