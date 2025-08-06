namespace BitcoderCZ.Fancade.Runtime;

/// <summary>
/// Provides methods to access and modify variable.
/// </summary>
public interface IVariableAccessor
{
    /// <summary>
    /// Gets the value of the variable at the specified index.
    /// </summary>
    /// <param name="variableId">Id of the variable.</param>
    /// <param name="index">The index within the variable of the value to get.</param>
    /// <returns>Value of the specified variable at <paramref name="index"/>.</returns>
    RuntimeValue GetVariableValue(int variableId, int index);

    /// <summary>
    /// Sets the value of a variable at the specified index.
    /// </summary>
    /// <param name="variableId">Id of the variable to update.</param>
    /// <param name="index">The index within the variable where the value should be set.</param>
    /// <param name="value">The new value.</param>
    void SetVariableValue(int variableId, int index, RuntimeValue value);
}
