using FancadeLoaderLib.Editing;
using MathUtils.Vectors;
using System.Diagnostics;

namespace FancadeLoaderLib.Runtime.Functions.Control;

public sealed class SwipeSensorFunction : IActiveFunction
{
    private static readonly byte3 SwipedPos = TerminalDef.GetOutPosition(0, 2, 2);
    private static readonly byte3 DirectionPos = TerminalDef.GetOutPosition(1, 2, 2);

    private float3 _direction;

    public int Execute(byte3 terminalPos, IRuntimeContext context, Span<byte3> executeNext)
    {
        Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be the before terminal.");

        int exeCount = 0;
        executeNext[exeCount++] = TerminalDef.AfterPosition;

        if (context.TryGetSwipe(out var direction))
        {
            _direction = direction;
            executeNext[exeCount++] = SwipedPos;
        }

        return exeCount;
    }

    public TerminalOutput GetTerminalOutput(byte3 terminalPos, IRuntimeContext context)
    {
        Debug.Assert(terminalPos == DirectionPos, $"{nameof(terminalPos)} should be valid.");

        return new TerminalOutput(new RuntimeValue(_direction));
    }
}
