using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Runtime.Exceptions;
using MathUtils.Vectors;
using System.Diagnostics;

namespace FancadeLoaderLib.Runtime.Functions.Variables;

public sealed class SetVariableFunction : IActiveFunction
{
    private readonly int _variableId;
    private readonly RuntimeTerminal _input;

    public SetVariableFunction(int variableId, RuntimeTerminal input)
    {
        _variableId = variableId;
        _input = input;
    }

    public int Execute(byte3 terminalPos, IRuntimeContext context, Span<byte3> executeNext)
    {
        Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(1), $"{nameof(terminalPos)} should be the before terminal.");

        context.SetVariableValue(_variableId, 0, _input.GetOutput(context).GetValue(context));

        executeNext[0] = TerminalDef.AfterPosition;

        return 1;
    }

    public TerminalOutput GetTerminalOutput(byte3 terminalPos, IRuntimeContext context)
        => throw new InvalidTerminalException(terminalPos);
}
