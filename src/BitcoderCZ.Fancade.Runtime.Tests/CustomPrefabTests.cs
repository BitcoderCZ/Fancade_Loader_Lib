using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Fancade.Editing.Scripting;
using BitcoderCZ.Fancade.Editing.Scripting.Builders;
using BitcoderCZ.Fancade.Editing.Scripting.Placers;
using BitcoderCZ.Fancade.Editing.Scripting.Terminals;
using BitcoderCZ.Fancade.Editing.Scripting.Utils;
using BitcoderCZ.Fancade.Partial;
using BitcoderCZ.Fancade.Runtime.Tests.Common;
using BitcoderCZ.Maths.Vectors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BitcoderCZ.BulletSharp.Dbvt;
using static BitcoderCZ.Fancade.Editing.Scripting.CodeWriter.Expressions;
using static BitcoderCZ.Fancade.Runtime.Tests.Common.ExeUtils;

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
        prefab.Connections.Add(new Connection(ushort3.One * Connection.IsFromToOutsideValue, ushort3.Zero, fromPos, StockBlocks.Control.If["Before"].Position));
        prefab.Connections.Add(new Connection(ushort3.Zero, ushort3.One * Connection.IsFromToOutsideValue, TerminalDef.AfterPosition, toPos));

        var builder = new PrefabBlockBuilder(level);

        ITerminal onPlay = NopTerminal.Instance;
        {
            var writer = new CodeWriter(new TowerCodePlacer(builder), new TerminalConnector(builder.Connect));
            writer.PlaySensor(writer =>
            {
                onPlay = writer.Connector.Store.Out[0];
            });
            writer.Flush();
        }

        var customBlock = new Block(new BlockDef(prefab.ToPartial(), ScriptBlockType.Active, PrefabTerminalInfo.Create(prefab, prefabs)), int3.Zero);
        builder.AddBlockSegments([customBlock]);

        ITerminal inspectInput = NopTerminal.Instance;
        {
            var writer = new CodeWriter(new TowerCodePlacer(builder), new TerminalConnector(builder.Connect));
            writer.Inspect(Number(1f));
            inspectInput = writer.Connector.Store.In;
            writer.Flush();
        }

        builder.Connect(onPlay, new BlockTerminal(customBlock, new TerminalDef(SignalType.Void, TerminalType.In, 0, fromPos)));
        builder.Connect(new BlockTerminal(customBlock, new TerminalDef(SignalType.Void, TerminalType.Out, 0, toPos)), inspectInput);

        builder.Build(int3.Zero);

        var compiled = Compile(prefabs);

        await Assert.That(compiled).Inspects([new InspectAssertExpected(1f) { Count = 1, Frequency = InspectFrequency.OnlyOnOneFrame }], runFor: 2);
    }
}
