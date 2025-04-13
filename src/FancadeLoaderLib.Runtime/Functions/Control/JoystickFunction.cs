using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Editing.Scripting.Settings;
using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace FancadeLoaderLib.Runtime.Functions.Control;

public sealed class JoystickFunction : IActiveFunction
{
    private readonly JoystickType _type;
    private float3 _direction;

    public JoystickFunction(JoystickType type)
    {
        _type = type;
    }

    public int Execute(byte3 terminalPos, IRuntimeContext context, Span<byte3> executeNext)
    {
        Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be the before terminal.");

        _direction = context.GetJoystickDirection(_type);

        executeNext[0] = TerminalDef.AfterPosition;

        return 1;
    }

    public TerminalOutput GetTerminalOutput(byte3 terminalPos, IRuntimeContext context)
    {
        Debug.Assert(terminalPos == TerminalDef.GetOutPosition(0, 2, 2), $"{nameof(terminalPos)} should be valid.");

        return new TerminalOutput(new RuntimeValue(_direction));
    }
}
