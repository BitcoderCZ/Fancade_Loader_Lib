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
    /// <param name="variableId">Id of the variable.</param>
    /// <param name="index">Index into the variable.</param>
    public VariableReference(int variableId, int index)
    {
        VariableId = variableId;
        Index = index;
    }

    /// <summary>
    /// Gets the id of the variable.
    /// </summary>
    /// <value>Id of the variable.</value>
    public readonly int VariableId { get; }

    /// <summary>
    /// Gets the index into the variable.
    /// </summary>
    /// <value>Index into the variable.</value>
    public readonly int Index { get; }

    /// <summary>
    /// Gets the value of the variable at <see cref="Index"/>.
    /// </summary>
    /// <param name="variableAccessor">The <see cref="IVariableAccessor"/> used to resolve the value.</param>
    /// <returns>Value of the <see cref="VariableReference"/>.</returns>
    public readonly RuntimeValue GetValue(IVariableAccessor variableAccessor)
        => variableAccessor.GetVariableValue(VariableId, Index);
}
