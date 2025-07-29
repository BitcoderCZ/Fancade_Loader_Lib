using BitcoderCZ.Fancade.Editing;

namespace BitcoderCZ.Fancade.Runtime;

public interface IAstRunner
{
    IEnumerable<Variable> GlobalVariables { get; }

    Action RunFrame();

    Span<RuntimeValue> GetGlobalVariableValue(Variable variable);
}
