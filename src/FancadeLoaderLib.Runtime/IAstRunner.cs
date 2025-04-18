using System;
using System.Collections.Generic;
using System.Text;

namespace FancadeLoaderLib.Runtime;

public interface IAstRunner
{
    IVariableAccessor VariableAccessor { get; }

    Action RunFrame();

    Variable GetVariable(int variableId);
}
