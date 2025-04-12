using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Runtime.Exceptions;
using MathUtils.Vectors;

namespace FancadeLoaderLib.Runtime.Functions.Math;

public sealed class ScreenToWorldFunction : BinaryFunction
{
    private static readonly byte3 WorldNearPos = TerminalDef.GetOutPosition(0, 2, 2);
    private static readonly byte3 WorldFarPos = TerminalDef.GetOutPosition(1, 2, 2);

    public ScreenToWorldFunction(RuntimeTerminal input1, RuntimeTerminal input2)
        : base(input1, input2)
    {
    }

    public override TerminalOutput GetTerminalOutput(byte3 terminalPos, IRuntimeContext context)
    {
        var (near, far) = context.ScreenToWorld(new float2(Input1.GetOutput(context).GetValue(context).Float, Input2.GetOutput(context).GetValue(context).Float));

        return terminalPos == WorldNearPos
            ? new TerminalOutput(new RuntimeValue(near))
            : terminalPos == WorldFarPos
            ? new TerminalOutput(new RuntimeValue(far))
            : throw new InvalidTerminalException(terminalPos);
    }
}
