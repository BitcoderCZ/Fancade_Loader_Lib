using FancadeLoaderLib.Editing;

namespace FancadeLoaderLib.Runtime;

public interface IAstRunner
{
    IEnumerable<Variable> GlobalVariables { get; }

    Action RunFrame();

    Span<RuntimeValue> GetGlobalVariableValue(Variable variable);

    IFcEnvironment GetEnvironment(int index);
}
