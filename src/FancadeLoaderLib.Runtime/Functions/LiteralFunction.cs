using FancadeLoaderLib.Editing;
using MathUtils.Vectors;
using System.Diagnostics;

namespace FancadeLoaderLib.Runtime.Functions;

public sealed class LiteralFunction : IFunction
{
    private readonly RuntimeValue _value;

    public LiteralFunction(RuntimeValue value)
    {
        _value = value;
    }

    public TerminalOutput GetTerminalValue(byte3 terminalPos, IRuntimeContext context)
    {
        Debug.Assert(terminalPos == TerminalDef.GetOutPosition(0, 2, 1), $"{nameof(terminalPos)} should be valid.");
        return new TerminalOutput(_value);
    }
}
