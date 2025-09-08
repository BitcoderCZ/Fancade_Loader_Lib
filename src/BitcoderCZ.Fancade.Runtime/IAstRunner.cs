using BitcoderCZ.Fancade.Editing;

namespace BitcoderCZ.Fancade.Runtime;

/// <summary>
/// Methods for running a <see cref="FcAST"/> and accessing its global variables.
/// </summary>
public interface IAstRunner
{
    /// <summary>
    /// Gets the global variables of the underlying <see cref="FcAST"/>.
    /// </summary>
    /// <value>Global variables of the underlying <see cref="FcAST"/>.</value>
    IEnumerable<Variable> GlobalVariables { get; }

    /// <summary>
    /// Gets the number of environments.
    /// </summary>
    /// <value>The number of environments.</value>
    int EnvironmentCount { get; }

    /// <summary>
    /// Runs a single frame.
    /// </summary>
    /// <returns>An <see cref="Action"/>, that when executed, runs the "Late Update" blocks.</returns>
    Action RunFrame();

    /// <summary>
    /// Gets the value of a global variable.
    /// </summary>
    /// <param name="variable">The variable whose value should be retrieved.</param>
    /// <returns>Value of <paramref name="variable"/>.</returns>
    Span<RuntimeValue> GetGlobalVariableValue(Variable variable);

    /// <summary>
    /// Gets the environment with the specified index.
    /// </summary>
    /// <param name="index">Index of the environment to retrieve.</param>
    /// <returns>An <see cref="IFcEnvironment"/> at the specified index.</returns>
    IFcEnvironment GetEnvironment(int index);
}
