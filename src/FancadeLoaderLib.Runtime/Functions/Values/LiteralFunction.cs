using FancadeLoaderLib.Editing;
using MathUtils.Vectors;
using System.Diagnostics;

namespace FancadeLoaderLib.Runtime.Functions.Values;

public sealed class LiteralFunction : IFunction
{
    private readonly RuntimeValue _value;
#if DEBUG
    private readonly bool _isBig;
#endif

    public LiteralFunction(RuntimeValue value, bool isBig)
    {
        _value = value;
#if DEBUG
        _isBig = isBig;
#endif
    }

    public TerminalOutput GetTerminalOutput(byte3 terminalPos, IRuntimeContext context)
    {
#if DEBUG
        Debug.Assert(terminalPos == TerminalDef.GetOutPosition(0, 2, _isBig ? 2 : 1), $"{nameof(terminalPos)} should be valid.");
#endif

        return new TerminalOutput(_value);
    }
}
