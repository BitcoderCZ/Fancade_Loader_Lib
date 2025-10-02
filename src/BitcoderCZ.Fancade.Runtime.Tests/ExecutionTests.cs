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

        writer.Inspect(Number(1f));
        writer.Inspect(Number(2f));
        //const string LoopStart = "LoopStart";
        //Variable index = new Variable("i", SignalType.Float);
        //writer.PlaySensor(writer =>
        //{
        //    writer.Label(LoopStart);
        //    writer.If(LessThan(Variable(index), Number(3f)),
        //    @true: writer =>
        //    {
        //        writer.Inspect(Variable(index));
        //        writer.IncrementNumber(Variable(index));
        //        writer.Goto(LoopStart);
        //    },
        //    @false: null);

        //    writer.Inspect(Number(999f));
        //});

        var compiled = Compile(writer, out var prefabs);
        FcAstCompiler.TryCompile(compiled, null!, new FcAstCompiler.Options(AssemblyLoadContext.Default)
        {
            StatementExecutionMode = FcAstCompiler.StatementExecutionMode.StateMachine,
            TerminalInfos = PrefabTerminalInfo.Create(prefabs),
        }, out string code, out _, out var diagnostics);
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

    private static CodeWriter CreateWriter()
        => CreateWriter(out _);

    private static CodeWriter CreateWriter(out Prefab prefab)
    {
        var builder = CreateBuilder(out prefab);
        var placer = new TowerCodePlacer(builder);
        placer.EnterStatementBlock();
        return new CodeWriter(placer, new TerminalConnector(builder.Connect));
    }

    private static PrefabBlockBuilder CreateBuilder(out Prefab prefab)
    {
        prefab = Prefab.CreateBlock(RawGame.CurrentNumbStockPrefabs, "A");
        var builder = new PrefabBlockBuilder(prefab);
        return builder;
    }

    private static FcAST Compile(CodeWriter writer, PrefabList prefabs, ushort? mainPrefabId = null)
    {
        writer.Flush();
        var prefab = (Prefab)writer.Placer.Builder.Build(int3.Zero);

        Debug.Assert(prefabs.ContainsPrefab(prefab.Id));
        prefabs.AddImplicitConnections();

        return FcAST.Parse(prefabs, mainPrefabId ?? prefab.Id);
    }

    private static FcAST Compile(CodeWriter writer)
        => Compile(writer, out _);

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