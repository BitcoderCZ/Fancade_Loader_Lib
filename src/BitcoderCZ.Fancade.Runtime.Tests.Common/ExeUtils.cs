using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Fancade.Editing.Scripting;
using BitcoderCZ.Fancade.Editing.Scripting.Builders;
using BitcoderCZ.Fancade.Editing.Scripting.Placers;
using BitcoderCZ.Fancade.Editing.Scripting.Utils;
using BitcoderCZ.Fancade.Raw;
using BitcoderCZ.Maths.Vectors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitcoderCZ.Fancade.Runtime.Tests.Common;

public static class ExeUtils
{
    public static CodeWriter CreateWriter()
        => CreateWriter(out _);

    public static CodeWriter CreateWriter(out Prefab prefab)
    {
        var builder = CreateBuilder(out prefab);
        var placer = new TowerCodePlacer(builder);
        placer.EnterStatementBlock();
        return new CodeWriter(placer, new TerminalConnector(builder.Connect));
    }

    public static CodeWriter CreateWriter(Prefab prefab)
    {
        var builder = new PrefabBlockBuilder(prefab);
        var placer = new TowerCodePlacer(builder);
        placer.EnterStatementBlock();
        return new CodeWriter(placer, new TerminalConnector(builder.Connect));
    }

    public static PrefabBlockBuilder CreateBuilder(out Prefab prefab)
    {
        prefab = Prefab.CreateBlock(RawGame.CurrentNumbStockPrefabs, "A");
        var builder = new PrefabBlockBuilder(prefab);
        return builder;
    }

    public static FcAST Compile(CodeWriter writer, PrefabList prefabs, ushort? mainPrefabId = null)
    {
        writer.Flush();
        var prefab = (Prefab)writer.Placer.Builder.Build(int3.Zero);

        Debug.Assert(prefabs.ContainsPrefab(prefab.Id));
        prefabs.AddImplicitConnections();

        return FcAST.Parse(prefabs, mainPrefabId ?? prefab.Id);
    }

    public static FcAST Compile(CodeWriter writer)
        => Compile(writer, out _);

    public static FcAST Compile(CodeWriter writer, out PrefabList prefabs)
    {
        writer.Flush();
        return Compile(writer.Placer.Builder, out prefabs);
    }

    public static FcAST Compile(BlockBuilder builder, out PrefabList prefabs)
    {
        prefabs = new PrefabList([(Prefab)builder.Build(int3.Zero)]);

        prefabs.AddImplicitConnections();

        return FcAST.Parse(prefabs, RawGame.CurrentNumbStockPrefabs);
    }

    public static FcAST Compile(PrefabList prefabs)
    {
        prefabs.AddImplicitConnections();

        return FcAST.Parse(prefabs, RawGame.CurrentNumbStockPrefabs);
    }
}
