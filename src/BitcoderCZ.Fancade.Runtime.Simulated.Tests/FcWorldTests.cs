using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Fancade.Editing.Scripting;
using BitcoderCZ.Fancade.Editing.Scripting.Builders;
using BitcoderCZ.Fancade.Editing.Scripting.Placers;
using BitcoderCZ.Fancade.Editing.Scripting.Terminals;
using BitcoderCZ.Fancade.Editing.Scripting.Utils;
using BitcoderCZ.Fancade.Editing.Utils;
using BitcoderCZ.Fancade.Raw;
using BitcoderCZ.Fancade.Runtime.Tests.Common;
using BitcoderCZ.Maths.Vectors;
using System.Numerics;
using static BitcoderCZ.Fancade.Editing.Scripting.CodeWriter.Expressions;

namespace BitcoderCZ.Fancade.Runtime.Simulated.Tests;

public class FcWorldTests
{
    [Test]
    public async Task Physics_ObjectFalls()
    {
        var writer = CreateWriter(out var prefab);

        var blocks = prefab.Blocks;
        blocks.SetPrefab(new int3(50, 1, 0), StockBlocks.Templates.PhysicsBox.Prefab);

        var terminal = new AbsolutePositionTerminal(new int3(50, 1, 0)) { VoxelPosition = int3.Zero };
        writer.PlaySensor(writer =>
        {
            writer.Inspect(GreaterThan(BreakVector(GetPos(terminal.Wrap()).Position).Y, Number(1.4f)));
        });

        writer.If(EqualsNumbers(CurrentFrame(), Number(120f)),
        @true: writer =>
        {
            writer.Inspect(LessThan(BreakVector(GetPos(terminal.Wrap()).Position).Y, Number(0.6f)));
        }, null);

        var compiled = Compile(writer, out var prefabs);

        await Assert.That(compiled).Inspects([new(true) { Count = 2 }], runFor: 121, physics: (prefab.Id, prefabs));
    }

    [Test]
    public async Task Physics_ObjectFalls_2()
    {
        var writer = CreateWriter(out var prefab);

        var blocks = prefab.Blocks;
        blocks.SetPrefab(new int3(50, 1, 1), StockBlocks.Templates.PhysicsBox.Prefab);

        var terminal = new AbsolutePositionTerminal(new int3(50, 1, 1)) { VoxelPosition = int3.Zero };
        writer.PlaySensor(writer =>
        {
            writer.Inspect(GreaterThan(BreakVector(GetPos(terminal.Wrap()).Position).Y, Number(1.4f)));
        });

        writer.If(EqualsNumbers(CurrentFrame(), Number(120f)),
        @true: writer =>
        {
            writer.Inspect(LessThan(BreakVector(GetPos(terminal.Wrap()).Position).Y, Number(0.6f)));
        }, null);

        var compiled = Compile(writer, out var prefabs);

        await Assert.That(compiled).Inspects([new(true) { Count = 2 }], runFor: 121, physics: (prefab.Id, prefabs));
    }

    [Test]
    public async Task Physics_NonPhysics_DoesNotMove()
    {
        var writer = CreateWriter(out var prefab);

        var blocks = prefab.Blocks;
        blocks.SetPrefab(new int3(50, 1, 0), StockBlocks.Templates.Box.Prefab);

        var terminal = new AbsolutePositionTerminal(new int3(50, 1, 0)) { VoxelPosition = int3.Zero };
        writer.If(EqualsNumbers(CurrentFrame(), Number(120f)),
        @true: writer =>
        {
            writer.Inspect(GreaterThan(BreakVector(GetPos(terminal.Wrap()).Position).Y, Number(1.4f)));
        }, null);

        var compiled = Compile(writer, out var prefabs);

        await Assert.That(compiled).Inspects([new(true) { Count = 2 }], runFor: 121, physics: (prefab.Id, prefabs));
    }

    [Test]
    public async Task Physics_Object_Rotates()
    {
        var writer = CreateWriter(out var prefab);

        var blocks = prefab.Blocks;
        blocks.SetPrefab(new int3(50, 1, 1), StockBlocks.Templates.PhysicsBox.Prefab);
        blocks.SetPrefab(new int3(50, 2, 1), StockBlocks.Templates.PhysicsBox.Prefab);
        blocks.SetPrefab(new int3(50, 2, 0), StockBlocks.Templates.PhysicsBox.Prefab);

        blocks.SetBlock(new int3(49, 0, 1), 1);
        blocks.SetBlock(new int3(51, 0, 1), 1);

        var terminal = new AbsolutePositionTerminal(new int3(50, 2, 0)) { VoxelPosition = int3.Zero };

        writer.If(EqualsNumbers(CurrentFrame(), Number(120f)),
        @true: writer =>
        {
            writer.Inspect(GetPos(terminal.Wrap()).Rotation);
        }, null);

        var compiled = Compile(writer, out var prefabs);

        await Assert.That(compiled).Inspects([new(Quaternion.CreateFromYawPitchRoll(0f, -45f * (float.Pi / 180f), 0f)) { Count = 1 }], runFor: 121, physics: (prefab.Id, prefabs));
    }

    [Test]
    public async Task ConnectionToBlock_ConnectsToCorrectBlock()
    {
        var writer = CreateWriter(out var prefab);

        var blocks = prefab.Blocks;
        blocks.SetPrefab(new int3(4, 2, 1), StockBlocks.Templates.Box.Prefab);
        blocks.SetPrefab(new int3(4, 0, 0), StockBlocks.Templates.Box.Prefab);
        blocks.SetPrefab(new int3(4, 0, 1), StockBlocks.Templates.Box.Prefab);

        var terminal = new AbsolutePositionTerminal(new int3(4, 2, 1)) { VoxelPosition = int3.Zero };
        writer.Inspect(GetPos(terminal.Wrap()).Position);

        var compiled = Compile(writer, out var prefabs);

        await Assert.That(compiled).Inspects([new(new Vector3(4.5f, 2.5f, 1.5f)) { Frequency = InspectFrequency.EveryFrame }], physics: (prefab.Id, prefabs));
    }

    [Test]
    public async Task ConnectionToBlock_ConnectsToCorrectBlock_2()
    {
        var writer = CreateWriter(out var prefab);

        var blocks = prefab.Blocks;
        blocks.SetPrefab(new int3(4, 2, 1), StockBlocks.Templates.Box.Prefab);
        blocks.SetPrefab(new int3(4, 0, 0), StockBlocks.Templates.Box.Prefab);
        blocks.SetPrefab(new int3(4, 0, 1), StockBlocks.Templates.Box.Prefab);

        var terminal = new AbsolutePositionTerminal(new int3(4, 0, 0)) { VoxelPosition = int3.Zero };
        writer.Inspect(GetPos(terminal.Wrap()).Position);

        var compiled = Compile(writer, out var prefabs);

        await Assert.That(compiled).Inspects([new(new Vector3(4.5f, 0.5f, 1f)) { Frequency = InspectFrequency.EveryFrame }], physics: (prefab.Id, prefabs));
    }

    private static CodeWriter CreateWriter(out Prefab prefab)
    {
        var builder = CreateBuilder(out prefab);
        var placer = new TowerCodePlacer(builder);
        placer.EnterStatementBlock();
        return new CodeWriter(placer, new TerminalConnector(builder.Connect));
    }

    private static PrefabBlockBuilder CreateBuilder(out Prefab prefab)
    {
        prefab = new Prefab(RawGame.CurrentNumbStockPrefabs);
        var builder = new PrefabBlockBuilder(prefab);
        return builder;
    }

    private static FcAST Compile(CodeWriter writer, out PrefabList prefabs)
    {
        writer.Flush();
        return Compile(writer.Placer.Builder, out prefabs);
    }

    private static FcAST Compile(BlockBuilder builder, out PrefabList prefabs)
    {
        prefabs = new PrefabList([(Prefab)builder.Build(int3.Zero)]);

        prefabs.AddImplicitConnections();

        return FcAST.Parse(prefabs, RawGame.CurrentNumbStockPrefabs);
    }
}
