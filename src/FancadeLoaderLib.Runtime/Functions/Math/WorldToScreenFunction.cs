using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Runtime.Exceptions;
using MathUtils.Vectors;

namespace FancadeLoaderLib.Runtime.Functions.Math;

public sealed class WorldToScreenFunction : UnaryFunction
{
    private static readonly byte3 ScreenXPos = TerminalDef.GetOutPosition(0, 2, 2);
    private static readonly byte3 ScreenYPos = TerminalDef.GetOutPosition(1, 2, 2);

    public WorldToScreenFunction(RuntimeTerminal input)
        : base(input)
    {
    }

    public override TerminalOutput GetTerminalOutput(byte3 terminalPos, IRuntimeContext context)
    {
        var screenPos = context.WorldToScreen(Input.GetOutput(context).GetValue(context).Float3);

        return terminalPos == ScreenXPos
           ? new TerminalOutput(new RuntimeValue(screenPos.X))
           : terminalPos == ScreenYPos
           ? new TerminalOutput(new RuntimeValue(screenPos.Y))
           : throw new InvalidTerminalException(terminalPos);
    }
}
