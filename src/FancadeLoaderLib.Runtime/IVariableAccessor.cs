using System;
using System.Collections.Generic;
using System.Text;

namespace FancadeLoaderLib.Runtime;

public interface IVariableAccessor
{
    RuntimeValue GetVariableValue(int variableId, int index);

    void SetVariableValue(int variableId, int index, RuntimeValue value);
}
