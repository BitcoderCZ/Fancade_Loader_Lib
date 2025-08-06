using System.Runtime.InteropServices;

namespace BitcoderCZ.Fancade.Runtime;

/// <summary>
/// Represents a reference to a variable along with its index.
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 8)]
public readonly struct VariableReference
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VariableReference"/> struct.
    /// </summary>
    /// <param name="variableId"></param>
    /// <param name="index"></param>
    public VariableReference(int variableId, int index)
    {
        VariableId = variableId;
        Index = index;
    }

    public readonly int VariableId { get; }

    public readonly int Index { get; }

    /// <summary>
    /// Gets the value of this of the variable at <see cref="Index"/>.
    /// </summary>
    /// <param name="variableAccessor"></param>
    /// <returns>Value of this <see cref="VariableReference"/>.</returns>
    public readonly RuntimeValue GetValue(IVariableAccessor variableAccessor)
        => variableAccessor.GetVariableValue(VariableId, Index);
}
