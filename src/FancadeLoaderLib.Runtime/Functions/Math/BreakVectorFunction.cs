using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Runtime.Exceptions;
using MathUtils.Vectors;

namespace FancadeLoaderLib.Runtime.Functions.Math;

public sealed class BreakVectorFunction : UnaryFunction
{
    private static readonly byte3 XPos = TerminalDef.GetOutPosition(0, 2, 3);
    private static readonly byte3 YPos = TerminalDef.GetOutPosition(1, 2, 3);
    private static readonly byte3 ZPos = TerminalDef.GetOutPosition(2, 2, 3);

    public BreakVectorFunction(RuntimeTerminal input)
        : base(input)
    {
    }

    public override TerminalOutput GetTerminalOutput(byte3 terminalPos, IRuntimeContext context)
    {
        var vector = Input.GetOutput(context).GetValue(context).Float3;

        return terminalPos == XPos
           ? new TerminalOutput(new RuntimeValue(vector.X))
           : terminalPos == YPos
           ? new TerminalOutput(new RuntimeValue(vector.Y))
           : terminalPos == ZPos
           ? new TerminalOutput(new RuntimeValue(vector.Y))
           : throw new InvalidTerminalException(terminalPos);
    }
}
