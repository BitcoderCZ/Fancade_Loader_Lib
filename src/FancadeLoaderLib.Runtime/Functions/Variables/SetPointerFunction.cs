using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Runtime.Exceptions;
using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace FancadeLoaderLib.Runtime.Functions.Variables;

public sealed class SetPointerFunction : IActiveFunction
{
    private readonly RuntimeTerminal _variable;
    private readonly RuntimeTerminal _value;

    public SetPointerFunction(RuntimeTerminal variable, RuntimeTerminal value)
    {
        _variable = variable;
        _value = value;
    }

    public int Execute(byte3 terminalPos, IRuntimeContext context, Span<byte3> executeNext)
    {
        Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be the before terminal.");

        var variableOutput = _variable.GetOutput(context);
        var valueOutput = _value.GetOutput(context);

        if (variableOutput.IsConnected && valueOutput.IsConnected)
        {
            var varRef = variableOutput.Reference;
            context.SetVariableValue(varRef.VariableId, varRef.Index, valueOutput.GetValue(context));
        }

        executeNext[0] = TerminalDef.AfterPosition;

        return 1;
    }

    public TerminalOutput GetTerminalOutput(byte3 terminalPos, IRuntimeContext context)
        => throw new InvalidTerminalException(terminalPos);
}
