using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Text;

namespace FancadeLoaderLib.Runtime;

public interface IFcEnvironment
{
    ushort PrefabId { get; }

    int Index { get; }

    int OuterEnvironmentIndex { get; }

    ushort3 OuterPosition { get; }
}
