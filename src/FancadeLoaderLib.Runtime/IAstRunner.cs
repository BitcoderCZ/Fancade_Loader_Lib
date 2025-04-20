using System;
using System.Collections.Generic;
using System.Text;

namespace FancadeLoaderLib.Runtime;

public interface IAstRunner
{
    IEnumerable<Variable> GlobalVariables { get; }

    Action RunFrame();

    Span<RuntimeValue> GetGlobalVariableValue(Variable variable);
}
