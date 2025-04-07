using FancadeLoaderLib.Runtime.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;
using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib.Runtime.Functions;

public sealed class SetVariableFunction : IActiveFunction
{
    private readonly int _variableId;
    private readonly RuntimeTerminal _input;

    public SetVariableFunction(int variableId, RuntimeTerminal input)
    {
        _variableId = variableId;
        _input = input;
    }

    public int Execute(IRuntimeContext context, Span<string> executeNext)
    {
        if (executeNext.Length < 0)
        {
            ThrowArgumentException($"{nameof(executeNext)}.Length must be at least 1.", nameof(executeNext));
        }

        context.SetVariableValue(_variableId, 0, _input.GetOutput(context).GetValue(context));

        executeNext[0] = "After";

        return 1;
    }

    public TerminalOutput GetTerminalValue(string name, IRuntimeContext context)
        => throw new InvalidTerminalException(name);
}
