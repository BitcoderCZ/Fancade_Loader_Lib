using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Text;

namespace FancadeLoaderLib.Runtime;

internal interface IFunction
{
    RuntimeValue GetTerminalValue(int3 terminalPos);
}
