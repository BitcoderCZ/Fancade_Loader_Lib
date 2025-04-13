using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Runtime.Exceptions;
using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace FancadeLoaderLib.Runtime.Functions.Variables;

public sealed class DecreaseNumberFunction : IActiveFunction
{
    private readonly RuntimeTerminal _variable;

    public DecreaseNumberFunction(RuntimeTerminal variable)
    {
        _variable = variable;
    }

    public int Execute(byte3 terminalPos, IRuntimeContext context, Span<byte3> executeNext)
    {
        Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(1), $"{nameof(terminalPos)} should be the before terminal.");

        var variableOutput = _variable.GetOutput(context);

        if (variableOutput.IsConnected)
        {
            var varRef = variableOutput.Reference;
            context.SetVariableValue(varRef.VariableId, varRef.Index, new RuntimeValue(context.GetVariableValue(varRef.VariableId, varRef.Index).Float - 1f));
        }

        executeNext[0] = TerminalDef.AfterPosition;

        return 1;
    }

    public TerminalOutput GetTerminalOutput(byte3 terminalPos, IRuntimeContext context)
        => throw new InvalidTerminalException(terminalPos);
}
