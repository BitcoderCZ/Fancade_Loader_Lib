using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Editing.Scripting.Settings;
using FancadeLoaderLib.Runtime.Exceptions;
using MathUtils.Vectors;
using System.Diagnostics;

namespace FancadeLoaderLib.Runtime.Functions.Control;

public sealed class ButtonFunction : IActiveFunction
{
    private static readonly byte3 ButtonPos = TerminalDef.GetOutPosition(0, 2, 2);

    private readonly ButtonType _type;

    public ButtonFunction(ButtonType type)
    {
        _type = type;
    }

    public int Execute(byte3 terminalPos, IRuntimeContext context, Span<byte3> executeNext)
    {
        Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be the before terminal.");

        int exeCount = 0;
        executeNext[exeCount++] = TerminalDef.AfterPosition;

        if (context.GetButtonPressed(_type))
        {
            executeNext[exeCount++] = ButtonPos;
        }

        return exeCount;
    }

    public TerminalOutput GetTerminalOutput(byte3 terminalPos, IRuntimeContext context)
        => throw new InvalidTerminalException(terminalPos);
}
