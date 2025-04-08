using FancadeLoaderLib.Editing;
using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace FancadeLoaderLib.Runtime.Functions;

public sealed class GetVariableFunction : IFunction
{
    private readonly int _variableId;

    public GetVariableFunction(int variableId)
    {
        _variableId = variableId;
    }

    public TerminalOutput GetTerminalValue(byte3 terminalPos, IRuntimeContext context)
    {
        Debug.Assert(terminalPos == TerminalDef.GetOutPosition(0, 2, 1), $"{nameof(terminalPos)} should be valid.");
        return new TerminalOutput(new VariableReference(_variableId, 0));
    }
}
