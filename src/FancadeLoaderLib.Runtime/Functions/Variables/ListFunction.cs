using FancadeLoaderLib.Editing;
using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace FancadeLoaderLib.Runtime.Functions.Variables;

public sealed class ListFunction : IFunction
{
    private readonly RuntimeTerminal _variable;
    private readonly RuntimeTerminal _index;

    public ListFunction(RuntimeTerminal variable, RuntimeTerminal index)
    {
        _variable = variable;
        _index = index;
    }

    public TerminalOutput GetTerminalOutput(byte3 terminalPos, IRuntimeContext context)
    {
        Debug.Assert(terminalPos == TerminalDef.GetOutPosition(0, 2, 2), $"{nameof(terminalPos)} should be valid.");

        var variableOutput = _variable.GetOutput(context);

        if (!variableOutput.IsConnected)
        {
            return TerminalOutput.Disconnected;
        }

        var varRef = variableOutput.Reference;

        return new TerminalOutput(new VariableReference(varRef.VariableId, varRef.Index + (int)_index.GetOutput(context).GetValue(context).Float));
    }
}
