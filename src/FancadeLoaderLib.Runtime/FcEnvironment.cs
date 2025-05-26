using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Text;

namespace FancadeLoaderLib.Runtime;

public sealed class FcEnvironment : IFcEnvironment
{
    public FcEnvironment(AST ast, int index, int outerEnvironmentIndex, ushort3 outerPosition)
    {
        Index = index;
        OuterEnvironmentIndex = outerEnvironmentIndex;
        AST = ast;
        OuterPosition = outerPosition;
    }

    public AST AST { get; }

    public int Index { get; }

    public int OuterEnvironmentIndex { get; }

    public ushort3 OuterPosition { get; }

    public Dictionary<ushort3, object> BlockData { get; } = [];

    public ushort PrefabId => AST.PrefabId;
}
