using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Text;

namespace FancadeLoaderLib.Runtime;

public interface IRuntimeGame
{
    float3 GetObjectPosition(int id);

    int CloneObject(int id);

    void InspectValue(int3 inspectBlockPos, RuntimeValue value);
}
