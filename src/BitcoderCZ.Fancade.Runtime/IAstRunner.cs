using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Fancade.Runtime.Exceptions;

namespace BitcoderCZ.Fancade.Runtime;

/// <summary>
/// Methods for running a <see cref="FcAST"/> and accessing its global variables.
/// </summary>
public interface IAstRunner : IDisposable
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
    /// <exception cref="FcTimeoutException">Thrown if the execution takes too long.</exception>
    /// <exception cref="InvalidInputException">Thrown when a script block receives invalid input.</exception>
    Action RunFrame();

    /// <summary>
    /// Resets the state of the <see cref="IAstRunner"/>.
    /// </summary>
    void Reset();

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
