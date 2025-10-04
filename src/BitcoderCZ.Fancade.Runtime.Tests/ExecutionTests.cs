using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Fancade.Editing.Scripting;
using BitcoderCZ.Fancade.Editing.Scripting.Builders;
using BitcoderCZ.Fancade.Editing.Scripting.Placers;
using BitcoderCZ.Fancade.Editing.Scripting.Terminals;
using BitcoderCZ.Fancade.Editing.Scripting.Utils;
using BitcoderCZ.Fancade.Editing.Utils;
using BitcoderCZ.Fancade.Raw;
using BitcoderCZ.Fancade.Runtime.Compiled;
using BitcoderCZ.Fancade.Runtime.Syntax;
using BitcoderCZ.Fancade.Runtime.Tests.Common;
using BitcoderCZ.Maths.Vectors;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.Loader;
using static BitcoderCZ.Fancade.Editing.Scripting.CodeWriter.Expressions;
using static BitcoderCZ.Fancade.Runtime.Tests.Common.ExeUtils;

namespace BitcoderCZ.Fancade.Runtime.Tests;

public partial class ExecutionTests
{
    [Test]
    [MethodDataSource(typeof(ExecutionTestsDataSources), nameof(ExecutionTestsDataSources.InspectableLiterals))]
    public async Task Inspect_InspectsEveryFrame(ObjectWrapper value)
    {
        var writer = CreateWriter();

        writer.Inspect(Literal(value.Object));

        var compiled = Compile(writer);

        await Assert.That(compiled).Inspects([new(value.Object) { Frequency = InspectFrequency.EveryFrame }]);
    }

    [Test]
    public async Task Execution_ExeConnectionsAreRespected()
    {
        var writer = CreateWriter();

        writer.Inspect(Literal(0f));
        writer.Inspect(Literal(1f));
        writer.Inspect(Literal(2f));

        var compiled = Compile(writer);

        await Assert.That(compiled).Inspects(
        [
            new(0f) { Order = 0, Frequency = InspectFrequency.EveryFrame },
            new(1f) { Order = 1, Frequency = InspectFrequency.EveryFrame },
            new(2f) { Order = 2, Frequency = InspectFrequency.EveryFrame },
        ]);
    }

    [Test]
    public async Task Execution_CorrectOrderByPlacement()
    {
        var builder = CreateBuilder(out _);

        List<Block> blocks = [];

        AddInspect(new int3(2, 0, 3), 0);
        AddInspect(new int3(2, 1, 0), 1);
        AddInspect(new int3(2, 0, 0), 2);
        AddInspect(new int3(7, 0, 0), 3);

        builder.AddBlockSegments(blocks);

        var compiled = Compile(builder, out _);

        await Assert.That(compiled).Inspects(
        [
            new(0f) { Order = 0, FrameCount = 1, },
            new(1f) { Order = 1, FrameCount = 1, },
            new(2f) { Order = 2, FrameCount = 1, },
            new(3f) { Order = 3, FrameCount = 1, },
        ]);

        void AddInspect(int3 pos, int count)
        {
            var inspect = new Block(StockBlocks.Values.Inspect_Number, pos);
            blocks.Add(inspect);

            var numb = new Block(StockBlocks.Values.Number, pos + new int3(-2, 0, 1));
            blocks.Add(numb);

            builder.SetSetting(numb, 0, (float)count);
        }
    }

    [Test]
    public async Task Execution_Connections_CorrectOrderByPlacement()
    {
        var builder = CreateBuilder(out _);

        List<Block> blocks = [];
        var playBlock = new Block(StockBlocks.Control.PlaySensor, new int3(0, 0, 10));
        blocks.Add(playBlock);
        var playTerminal = new BlockTerminal(playBlock, "On Play");

        AddInspect(new int3(2, 0, 3), 0);
        AddInspect(new int3(2, 1, 0), 1);
        AddInspect(new int3(2, 0, 0), 2);
        AddInspect(new int3(7, 0, 0), 3);

        builder.AddBlockSegments(blocks);

        var compiled = Compile(builder, out _);

        await Assert.That(compiled).Inspects(
        [
            new(0f) { Order = 0, Count = 1, Frequency = InspectFrequency.OnlyOnOneFrame, },
            new(1f) { Order = 1, Count = 1, Frequency = InspectFrequency.OnlyOnOneFrame, },
            new(2f) { Order = 2, Count = 1, Frequency = InspectFrequency.OnlyOnOneFrame, },
            new(3f) { Order = 3, Count = 1, Frequency = InspectFrequency.OnlyOnOneFrame, },
        ]);

        void AddInspect(int3 pos, int count)
        {
            var inspect = new Block(StockBlocks.Values.Inspect_Number, pos);
            blocks.Add(inspect);

            var numb = new Block(StockBlocks.Values.Number, pos + new int3(-2, 0, 1));
            blocks.Add(numb);

            builder.Connect(playTerminal, new BlockTerminal(inspect, "Before"));
            builder.SetSetting(numb, 0, (float)count);
        }
    }

    [Test]
    public async Task StockBlocks_HaveImplicitConnections()
    {
        var list = new PrefabList();
        var prefab = Prefab.CreateLevel(0, "A");
        list.AddPrefab(prefab);

        var blocks = prefab.Blocks;
        blocks.SetPrefab(int3.Zero, StockBlocks.PrefabList.GetPrefab(540)); // Camera Orbit

        var ast = FcAST.Parse(list, prefab.Id);
        await Assert.That(((CustomStatementSyntax)ast.Statements.First().Value).AST.EntryPointTerminals.Length).IsEqualTo(6);
    }

    [Test]
    public async Task PlaySensor_ExecutedOnlyOnFirstFrame()
    {
        var writer = CreateWriter();

        writer.PlaySensor(writer =>
        {
            writer.Inspect(Number(1f));
        });

        var compiled = Compile(writer);

        await Assert.That(compiled).Inspects([new(1f) { Count = 1, }], runFor: 2);
    }

    [Test]
    public async Task BoxArtSensor_ExecutedOnlyWhenTakingBoxArt()
    {
        var writer = CreateWriter();

        writer.BoxArtSensor(writer =>
        {
            writer.Inspect(Number(1f));
        });

        var compiled = Compile(writer);

        await Assert.That(compiled).Inspects([new(1f) { BoxArt = true, Count = 2 }], runFor: 2);
    }

    [Test]
    public async Task IfGotoLoop()
    {
        var writer = CreateWriter();

        const string LoopStart = "LoopStart";
        Variable index = new Variable("i", SignalType.Float);
        writer.PlaySensor(writer =>
        {
            writer.Label(LoopStart);
            writer.If(LessThan(Variable(index), Number(3f)),
            @true: writer =>
            {
                writer.Inspect(Variable(index));
                writer.IncrementNumber(Variable(index));
                writer.Goto(LoopStart);
            },
            @false: writer =>
            {
                writer.Inspect(Number(222f));
            });

            writer.Inspect(Number(111f));
        });

        var compiled = Compile(writer, out var prefabs);

        await Assert.That(compiled).Inspects([
            new InspectAssertExpected(0f) {Frequency = InspectFrequency.OnlyOnOneFrame, Count = 1, Order = 0 },
            new InspectAssertExpected(1f) {Frequency = InspectFrequency.OnlyOnOneFrame, Count = 1, Order = 1 },
            new InspectAssertExpected(2f) {Frequency = InspectFrequency.OnlyOnOneFrame, Count = 1, Order = 2 },
            new InspectAssertExpected(111f) {Frequency = InspectFrequency.OnlyOnOneFrame, Count = 4 },
            new InspectAssertExpected(222f) {Frequency = InspectFrequency.OnlyOnOneFrame, Count = 1, Order = 3 },
        ], runFor: 2);
    }

    [Test]
    public async Task Loop_Increasing_CountIsCorrect()
    {
        var writer = CreateWriter();

        writer.Loop(Number(0f), Number(10f), (writer, counter) =>
        {
            writer.Inspect(counter.Wrap());
        });

        var compiled = Compile(writer);

        await Assert.That(compiled).Inspects(Enumerable.Range(0, 10).Select(i => new InspectAssertExpected((float)i) { Order = i, FrameCount = 1 }));
    }

    [Test]
    public async Task Loop_Decreasing_CountIsCorrect()
    {
        var writer = CreateWriter();

        writer.Loop(Number(10f), Number(0f), (writer, counter) =>
        {
            writer.Inspect(counter.Wrap());
        });

        var compiled = Compile(writer);

        await Assert.That(compiled).Inspects(Enumerable.Range(1, 10).Select(i => new InspectAssertExpected((float)(11 - i)) { Order = i, FrameCount = 1 }));
    }

    [Test]
    public async Task Random_ValueIsCorrect()
    {
        var writer = CreateWriter();

        writer.RandomSeed(Number(0f));
        writer.Inspect(Random(None(), None()));
        writer.Inspect(Random(None(), None()));

        var compiled = Compile(writer);

        var rng = new FcRandom();
        rng.SetSeed(0f);

        await Assert.That(compiled).Inspects([new(rng.NextSingle()) { Count = 2 }, new(rng.NextSingle()) { Count = 2 }], runFor: 2);
    }

    [Test]
    public async Task DisconnectedOutsideTerminals_ReturnsDefaultValue()
    {
        var writer = CreateWriter(out var prefab);
        prefab[int3.Zero].Voxels[int3.Zero] = new Voxel(FcColor.Black, false);

        var prefabs = new PrefabList();

        var level = Prefab.CreateLevel(0, "A");
        prefabs.AddPrefab(level);
        prefabs.AddPrefab(prefab);

        var blocks = level.Blocks;
        blocks.SetPrefab(new int3(0, 0, 0), prefab);

        var terminal = new AbsolutePositionTerminal(new int3(Connection.IsFromToOutsideValue, Connection.IsFromToOutsideValue, Connection.IsFromToOutsideValue)) { VoxelPosition = int3.Zero };
        writer.Inspect(terminal.Wrap(), SignalType.Rot);

        var compiled = Compile(writer, prefabs, level.Id);

        await Assert.That(compiled).Inspects([new(Quaternion.Identity) { Count = 2 }], runFor: 2);
    }
}

public static class ExecutionTestsDataSources
{
    public static IEnumerable<Func<ObjectWrapper>> InspectableLiterals()
    {
        yield return () => new(5.5f);
        yield return () => new(new Vector3(1f, 2f, 3f));
        yield return () => new(new Rotation(new Vector3(45f, 90f, 270f)));
        yield return () => new(true);
    }
}

public readonly struct ObjectWrapper
{
    public readonly object Object;

    public ObjectWrapper(object @object)
    {
        Object = @object;
    }
}