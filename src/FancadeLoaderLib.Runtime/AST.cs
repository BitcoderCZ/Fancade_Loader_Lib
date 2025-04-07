using System;
using System.Collections.Generic;
using System.Text;

namespace FancadeLoaderLib.Runtime;

public sealed class AST
{
    private List<object> _entryPoints;

    private AST()
    {
    }

    public static AST Create(PrefabList prefabs, ushort mainId, IRuntimeGame game)
    {

    }
}