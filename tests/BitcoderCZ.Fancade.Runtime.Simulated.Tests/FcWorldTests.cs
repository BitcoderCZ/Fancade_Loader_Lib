using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Fancade.Editing.Scripting;
using BitcoderCZ.Fancade.Editing.Scripting.Emitters;
using BitcoderCZ.Fancade.Editing.Scripting.Positioners;
using BitcoderCZ.Fancade.Editing.Utils;
using BitcoderCZ.Fancade.Runtime.Tests.Common;
using BitcoderCZ.Maths.Vectors;
using System.Numerics;
using static BitcoderCZ.Fancade.Editing.Scripting.CodeWriter.Expressions;
using static BitcoderCZ.Fancade.Editing.Scripting.Node;

namespace BitcoderCZ.Fancade.Runtime.Simulated.Tests;

public class FcWorldTests
{
    // [Test]
    // public async Task Physics_ObjectFalls()
    // {
    //     var writer = new CodeWriter(new CodeGraph.Builder());

    //     var prefab = Prefab.CreateLevel(0, "A");
    //     var blocks = prefab.Blocks;
    //     blocks.SetPrefab(new int3(50, 1, 0), StockBlocks.Templates.PhysicsBox.Prefab);

    //     var terminal = Terminal.ObjectAbsolute(new int3(50, 1, 0), byte3.Zero);
    //     writer.PlaySensor(writer =>
    //     {
    //         writer.Inspect(GreaterThan(BreakVector(GetPos(terminal.Wrap()).Position).Y, Number(1.4f)));
    //     });

    //     writer.If(EqualsNumbers(CurrentFrame(), Number(120f)),
    //     @true: writer =>
    //     {
    //         writer.Inspect(LessThan(BreakVector(GetPos(terminal.Wrap()).Position).Y, Number(0.6f)));
    //     }, null);

    //     var tester = AstRunnerTester.CreatePhysics(writer, prefab, options: new() { RunFor = 121, });

    //     await Assert.That(tester).Inspects(new(true) { Count = 2 });
    // }

    [Test]
    public async Task Physics_ObjectFalls_2()
    {
        var writer = new CodeWriter(new CodeGraph.Builder());

        var prefab = Prefab.CreateLevel(0, "A");
        var blocks = prefab.Blocks;
        blocks.SetPrefab(new int3(50, 1, 1), StockBlocks.Templates.PhysicsBox.Prefab);

        var terminal = Terminal.ObjectAbsolute(new int3(50, 1, 1), byte3.Zero);

        writer.PlaySensor(writer =>
        {
            writer.Inspect(GreaterThan(BreakVector(GetPos(terminal.Wrap()).Position).Y, Number(1.4f)));
        });

        writer.If(EqualsNumbers(CurrentFrame(), Number(120f)),
        @true: writer =>
        {
            writer.Inspect(LessThan(BreakVector(GetPos(terminal.Wrap()).Position).Y, Number(0.6f)));
        }, null);

        var tester = AstRunnerTester.CreatePhysics(writer, prefab, options: new() { RunFor = 121, });

        await Assert.That(tester).Inspects(new(true) { Count = 2 });
    }

    [Test]
    public async Task Physics_NonPhysics_DoesNotMove()
    {
        var writer = new CodeWriter(new CodeGraph.Builder());

        var prefab = Prefab.CreateLevel(0, "A");
        var blocks = prefab.Blocks;
        blocks.SetPrefab(new int3(50, 1, 0), StockBlocks.Templates.Box.Prefab);

        var terminal = Terminal.ObjectAbsolute(new int3(50, 1, 0), byte3.Zero);
        writer.If(EqualsNumbers(CurrentFrame(), Number(120f)),
        @true: writer =>
        {
            writer.Inspect(GreaterThan(BreakVector(GetPos(terminal.Wrap()).Position).Y, Number(1.4f)));
        }, null);

        var tester = AstRunnerTester.CreatePhysics(writer, prefab, options: new() { RunFor = 121, });

        await Assert.That(tester).Inspects(new(true) { Count = 2 });
    }

    [Test]
    public async Task Physics_Object_Rotates()
    {
        var writer = new CodeWriter(new CodeGraph.Builder());

        var prefab = Prefab.CreateLevel(0, "A");
        var blocks = prefab.Blocks;
        blocks.SetPrefab(new int3(50, 1, 1), StockBlocks.Templates.PhysicsBox.Prefab);
        blocks.SetPrefab(new int3(50, 2, 1), StockBlocks.Templates.PhysicsBox.Prefab);
        blocks.SetPrefab(new int3(50, 2, 0), StockBlocks.Templates.PhysicsBox.Prefab);

        blocks.SetBlock(new int3(49, 0, 1), 1);
        blocks.SetBlock(new int3(51, 0, 1), 1);

        var terminal = Terminal.ObjectAbsolute(new int3(50, 2, 0), byte3.Zero);

        writer.If(EqualsNumbers(CurrentFrame(), Number(120f)),
        @true: writer =>
        {
            writer.Inspect(GetPos(terminal.Wrap()).Rotation);
        }, null);

        var tester = AstRunnerTester.CreatePhysics(writer, prefab, options: new() { RunFor = 121, });

        await Assert.That(tester).Inspects(new(Quaternion.CreateFromYawPitchRoll(0f, -45f * (float.Pi / 180f), 0f)) { Count = 1 });
    }

    [Test]
    public async Task ConnectionToBlock_ConnectsToCorrectBlock()
    {
        var writer = new CodeWriter(new CodeGraph.Builder());

        var prefab = Prefab.CreateLevel(0, "A");
        var blocks = prefab.Blocks;
        blocks.SetPrefab(new int3(4, 2, 1), StockBlocks.Templates.Box.Prefab);
        blocks.SetPrefab(new int3(4, 0, 0), StockBlocks.Templates.Box.Prefab);
        blocks.SetPrefab(new int3(4, 0, 1), StockBlocks.Templates.Box.Prefab);

        var terminal = Terminal.ObjectAbsolute(new int3(4, 2, 1), byte3.Zero);
        writer.Inspect(GetPos(terminal.Wrap()).Position);

        var tester = AstRunnerTester.CreatePhysics(writer, prefab);

        await Assert.That(tester).Inspects(new(new Vector3(4.5f, 2.5f, 1.5f)) { Frequency = InspectFrequency.EveryFrame });
    }

    [Test]
    public async Task ConnectionToBlock_ConnectsToCorrectBlock_2()
    {
        var writer = new CodeWriter(new CodeGraph.Builder());

        var prefab = Prefab.CreateLevel(0, "A");
        var blocks = prefab.Blocks;
        blocks.SetPrefab(new int3(4, 2, 1), StockBlocks.Templates.Box.Prefab);
        blocks.SetPrefab(new int3(4, 0, 0), StockBlocks.Templates.Box.Prefab);
        blocks.SetPrefab(new int3(4, 0, 1), StockBlocks.Templates.Box.Prefab);

        var terminal = Terminal.ObjectAbsolute(new int3(4, 0, 0), byte3.Zero);
        writer.Inspect(GetPos(terminal.Wrap()).Position);

        var tester = AstRunnerTester.CreatePhysics(writer, prefab);

        await Assert.That(tester).Inspects(new(new Vector3(4.5f, 0.5f, 1f)) { Frequency = InspectFrequency.EveryFrame });
    }

    [Test]
    public async Task ConnectionToBlock_ConnectsToCorrectBlock_3()
    {
        var writer = new CodeWriter(new CodeGraph.Builder());
        var prefab = Prefab.CreateBlock(0, "A");
        prefab[int3.Zero].Voxels.Fill(new Voxel(FcColor.Black, false));

        var prefabs = new PrefabList();

        var level = Prefab.CreateLevel(0, "A");
        prefabs.AddPrefab(level);
        prefabs.AddPrefab(prefab);

        var block = Prefab.CreateBlock(0, "A");
        prefabs.AddPrefab(block);
        var voxels = block[int3.Zero].Voxels;
        for (int z = 0; z < 8; z++)
        {
            for (int x = 0; x < 8; x++)
            {
                voxels[new int3(x, 0, z)] = new Voxel(FcColor.Black, false);
            }
        }

        var blocks = level.Blocks;
        for (int z = 0; z < 3; z++)
        {
            for (int x = 0; x < 3; x++)
            {
                blocks.SetPrefab(new int3(x, 0, z), block);
            }
        }

        blocks.SetPrefab(new int3(1, 1, 1), prefab);

        var terminal = Terminal.CreateIn(byte3.One, 0, SignalType.Obj);
        var size = GetSize(terminal.Wrap());
        writer.Inspect(SubtractVectors(size.Max, size.Min));

        PrefabCodeGraphEmitter.Emit(TowerCodeGraphPositioner.Layout(writer.Builder.BuildAndClear()), prefab, int3.Zero);

        var tester = AstRunnerTester.CreatePhysics(prefabs, level.Id);

        await Assert.That(tester).Inspects(new(new Vector3(1f, 1f, 1f)) { Frequency = InspectFrequency.EveryFrame });
    }

    [Test]
    public async Task ConnectionToSelf_ReferencesSelf()
    {
        var writer = new CodeWriter(new CodeGraph.Builder());
        var prefab = Prefab.CreateBlock(0, "A");
        prefab[int3.Zero].Voxels.Fill(new Voxel(FcColor.Black, false));

        var prefabs = new PrefabList();

        var level = Prefab.CreateLevel(0, "A");
        prefabs.AddPrefab(level);
        prefabs.AddPrefab(prefab);

        var blocks = level.Blocks;
        blocks.SetPrefab(new int3(0, 0, 0), prefab);

        var terminal = Terminal.CreateIn(byte3.One, 0, SignalType.Obj);
        writer.Inspect(terminal.Wrap(), SignalType.Obj);

        PrefabCodeGraphEmitter.Emit(TowerCodeGraphPositioner.Layout(writer.Builder.BuildAndClear()), prefab, int3.Zero);

        var tester = AstRunnerTester.CreatePhysics(prefabs, level.Id);

        await Assert.That(tester).Inspects(new(new FcObject(1)) { Frequency = InspectFrequency.EveryFrame });
    }

    [Test]
    public async Task ConnectionToSelf_ReferencesSelf2()
    {
        var writer = new CodeWriter(new CodeGraph.Builder());
        var prefab = Prefab.CreateBlock(0, "A");
        var voxels = prefab[int3.Zero].Voxels;
        var voxel = new Voxel(FcColor.Black, false);
        voxels[new int3(1, 1, 1)] = voxel;
        voxels[new int3(2, 1, 1)] = voxel;
        voxels[new int3(2, 1, 2)] = voxel;

        var prefabs = new PrefabList();

        var level = Prefab.CreateLevel(0, "A");
        prefabs.AddPrefab(level);
        prefabs.AddPrefab(prefab);

        var blocks = level.Blocks;
        blocks.SetBlock(new int3(0, 0, 0), 1);
        blocks.SetPrefab(new int3(1, 0, 0), prefab);

        var terminal = Terminal.CreateIn(new byte3(2, 1, 1), 0, SignalType.Obj);
        writer.Inspect(terminal.Wrap(), SignalType.Obj);
        PrefabCodeGraphEmitter.Emit(TowerCodeGraphPositioner.Layout(writer.Builder.BuildAndClear()), prefab, int3.Zero);

        var tester = AstRunnerTester.CreatePhysics(prefabs, level.Id);

        await Assert.That(tester).Inspects(new(new FcObject(2)) { Frequency = InspectFrequency.EveryFrame });
    }
}
