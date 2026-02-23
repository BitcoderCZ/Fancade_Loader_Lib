using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Fancade.Editing.Scripting;
using BitcoderCZ.Fancade.Editing.Scripting.Utils;
using BitcoderCZ.Fancade.Partial;
using BitcoderCZ.Fancade.Runtime.Tests.Common;
using BitcoderCZ.Maths.Vectors;
using Terminal = BitcoderCZ.Fancade.Editing.Scripting.Node.Terminal;
using static BitcoderCZ.Fancade.Editing.Scripting.CodeWriter.Expressions;
using BitcoderCZ.Fancade.Editing.Scripting.Emitters;
using BitcoderCZ.Fancade.Editing.Scripting.Positioners;

namespace BitcoderCZ.Fancade.Runtime.Tests;

public class CustomPrefabTests
{
    [Test]
    public async Task CustomPrefab_Void_BeforeAfter()
    {
        var prefabs = new PrefabList();
        var level = Prefab.CreateLevel(0, "A");
        var prefab = Prefab.CreateBlock(0, "A");
        prefab[int3.Zero].Voxels.Fill(new Voxel(FcColor.Black, false));
        prefabs.AddPrefab(level);
        prefabs.AddPrefab(prefab);

        var fromPos = new byte3(3, 1, 7);
        var toPos = new byte3(3, 1, 0);
        prefab.Blocks.SetPrefab(int3.Zero, StockBlocks.Control.If.Prefab);
        prefab.Connections.Add(new Connection(int3.One * Connection.IsFromToOutsideValue, int3.Zero, fromPos, StockBlocks.Control.If["Before"].Position));
        prefab.Connections.Add(new Connection(int3.Zero, int3.One * Connection.IsFromToOutsideValue, TerminalDef.AfterPosition, toPos));

        var builder = new CodeGraph.Builder();

        Terminal onPlay = Terminal.Null;
        {
            using var writer = new CodeWriter(builder);
            writer.PlaySensor(writer =>
            {
                onPlay = writer.Connector.Store.Out[0];
            });
        }

        var customBlock = new Block(new BlockDef(prefab.ToPartial(), ScriptBlockType.Active, PrefabTerminalInfo.Create(prefab, prefabs)), int3.Zero);
        var region = builder.CreateRegion(int3.One);
        region.Blocks[int3.Zero] = prefab.Id;

        Terminal inspectInput = Terminal.Null;
        {
            using var writer = new CodeWriter(builder);
            writer.Inspect(Number(1f));
            inspectInput = writer.Connector.Store.In;
        }

        builder.Connect(onPlay, Terminal.ObjectRelative(region.Handle, int3.Zero, fromPos, SignalType.Void));
        builder.Connect(Terminal.ObjectRelative(region.Handle, int3.Zero, toPos, SignalType.Void), inspectInput);

        PrefabCodeGraphEmitter.Emit(TowerCodeGraphPositioner.Layout(builder.BuildAndClear()), level, int3.Zero);

        var tester = AstRunnerTester.Create(prefabs, options: new() { RunFor = 2, });

        await Assert.That(tester).Inspects(new InspectAssertExpected(1f) { Count = 1, Frequency = InspectFrequency.OnlyOnOneFrame });
    }

    [Test]
    public async Task CustomPrefab_Execution_CorrectOrderByPlacement()
    {
        var level = Prefab.CreateLevel(0, "A");
        var prefabs = new PrefabList();
        prefabs.AddPrefab(level);

        AddInspect(new int3(2, 0, 10), 0);

        AddCustomInspect(new int3(7, 0, 3), 4);
        AddCustomInspect(new int3(2, 0, 3), 3);
        AddCustomInspect(new int3(2, 1, 3), 2);
        AddCustomInspect(new int3(2, 0, 6), 1);

        AddInspect(new int3(2, 0, 0), 5);

        var tester = AstRunnerTester.Create(prefabs);

        await Assert.That(tester)
            .Inspects(new(0f) { Order = 0, FrameCount = 1, })
            .And.Inspects(new(1f) { Order = 1, FrameCount = 1, })
            .And.Inspects(new(2f) { Order = 2, FrameCount = 1, })
            .And.Inspects(new(3f) { Order = 3, FrameCount = 1, })
            .And.Inspects(new(4f) { Order = 4, FrameCount = 1, })
            .And.Inspects(new(5f) { Order = 5, FrameCount = 1, });

        void AddInspect(int3 pos, int count)
        {
            level.Blocks.SetPrefab(pos, StockBlocks.Values.Inspect_Number.Prefab);

            var numbPos = pos + new int3(-2, 0, 1);
            level.Blocks.SetPrefab(numbPos, StockBlocks.Values.Number.Prefab);

            level.Settings[numbPos] = new PrefabSettings(new PrefabSetting(0, (float)count));
        }

        void AddCustomInspect(int3 pos, int count)
        {
            var prefab = Prefab.CreateBlock(0, "A");
            prefabs.AddPrefab(prefab);

            prefab.Blocks.SetPrefab(pos, StockBlocks.Values.Inspect_Number.Prefab);

            var numbPos = pos + new int3(-2, 0, 1);
            prefab.Blocks.SetPrefab(numbPos, StockBlocks.Values.Number.Prefab);

            prefab.Settings[numbPos] = new PrefabSettings(new PrefabSetting(0, (float)count));

            level.Blocks.SetPrefab(pos, prefab);
        }
    }
}
