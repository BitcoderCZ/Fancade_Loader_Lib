using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Runtime.Exceptions;
using MathUtils.Vectors;
using System.Diagnostics;

namespace FancadeLoaderLib.Runtime.Functions.Control;

public sealed class BoxArtSensorFunction : IActiveFunction
{
    private static readonly byte3 OnScreenshotPos = TerminalDef.GetOutPosition(0, 2, 2);

    public int Execute(byte3 terminalPos, IRuntimeContext context, Span<byte3> executeNext)
    {
        Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be the before terminal.");

        int exeCount = 0;
        executeNext[exeCount++] = TerminalDef.AfterPosition;

        if (context.TakingBoxArt)
        {
            executeNext[exeCount++] = OnScreenshotPos;
        }

        return exeCount;
    }

    public TerminalOutput GetTerminalOutput(byte3 terminalPos, IRuntimeContext context)
        => throw new InvalidTerminalException(terminalPos);
}
