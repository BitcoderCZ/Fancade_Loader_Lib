using System;
using System.Collections.Generic;
using System.Text;

namespace FancadeLoaderLib.Runtime;

internal interface IActiveFunction : IFunction
{
    void Execute();
}
