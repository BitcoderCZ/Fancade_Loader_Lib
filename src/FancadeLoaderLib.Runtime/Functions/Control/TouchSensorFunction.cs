using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Editing.Scripting.Settings;
using FancadeLoaderLib.Runtime.Exceptions;
using MathUtils.Vectors;
using System.Diagnostics;
using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib.Runtime.Functions.Control;

public sealed class TouchSensorFunction : IActiveFunction
{
    private static readonly byte3 TouchedPos = TerminalDef.GetOutPosition(0, 2, 3);
    private static readonly byte3 ScreenXPos = TerminalDef.GetOutPosition(1, 2, 3);
    private static readonly byte3 ScreenYPos = TerminalDef.GetOutPosition(2, 2, 3);

    private readonly TouchState _state;
    private readonly int _fingerIndex;
    private float2 _touchPos;

    public TouchSensorFunction(TouchState state, int fingerIndex)
    {
        if (fingerIndex < 0 || fingerIndex > 2)
        {
            ThrowArgumentOutOfRangeException($"{nameof(fingerIndex)} must be between 0 and 2.", nameof(fingerIndex));
        }

        _state = state;
        _fingerIndex = fingerIndex;
    }

    public int Execute(byte3 terminalPos, IRuntimeContext context, Span<byte3> executeNext)
    {
        Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(3), $"{nameof(terminalPos)} should be the before terminal.");

        int exeCount = 0;
        executeNext[exeCount++] = TerminalDef.AfterPosition;

        if (context.TryGetTouch(_state, _fingerIndex, out var touchPos))
        {
            _touchPos = touchPos;
            executeNext[exeCount++] = TouchedPos;
        }

        return exeCount;
    }

    public TerminalOutput GetTerminalOutput(byte3 terminalPos, IRuntimeContext context)
        => terminalPos == ScreenXPos
            ? new TerminalOutput(new RuntimeValue(_touchPos.X))
            : terminalPos == ScreenYPos
            ? new TerminalOutput(new RuntimeValue(_touchPos.Y))
            : throw new InvalidTerminalException(terminalPos);
}
