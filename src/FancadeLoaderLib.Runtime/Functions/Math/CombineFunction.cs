using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Runtime.Utils;
using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;

namespace FancadeLoaderLib.Runtime.Functions.Math;

public sealed class CombineFunction : BinaryFunction
{
    public CombineFunction(RuntimeTerminal input1, RuntimeTerminal input2)
       : base(input1, input2)
    {
    }

    public override TerminalOutput GetTerminalOutput(byte3 terminalPos, IRuntimeContext context)
    {
        Debug.Assert(terminalPos == TerminalDef.GetOutPosition(0, 2, 2), $"{nameof(terminalPos)} should be valid.");

        return new TerminalOutput(new RuntimeValue(Input1.GetOutput(context).GetValue(context).Quaternion * Input2.GetOutput(context).GetValue(context).Quaternion));
    }
}
