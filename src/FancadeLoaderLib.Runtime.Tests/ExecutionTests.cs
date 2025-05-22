using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Editing.Scripting;
using FancadeLoaderLib.Editing.Scripting.Builders;
using FancadeLoaderLib.Editing.Scripting.Placers;
using FancadeLoaderLib.Editing.Scripting.Utils;
using FancadeLoaderLib.Raw;
using FancadeLoaderLib.Runtime.Tests.AssertUtils;
using MathUtils.Vectors;
using static FancadeLoaderLib.Editing.Scripting.CodeWriter.Expressions;

namespace FancadeLoaderLib.Runtime.Tests;

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
        var builder = CreateBuilder();

        List<Block> blocks = [];

        AddInspect(new int3(2, 0, 3), 0);
        AddInspect(new int3(2, 1, 0), 1);
        AddInspect(new int3(2, 0, 0), 2);
        AddInspect(new int3(7, 0, 0), 3);

        builder.AddBlockSegments(blocks);

        var compiled = Compile(builder);

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
    public async Task Loop_Increasing_CountIsCorrect()
    {
        var writer = CreateWriter();

        writer.Loop(Number(0f), Number(10f), (writer, counter) =>
        {
            writer.Inspect(WrapTerminal(counter));
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
            writer.Inspect(WrapTerminal(counter));
        });

        var compiled = Compile(writer);

        await Assert.That(compiled).Inspects(Enumerable.Range(1, 10).Select(i => new InspectAssertExpected((float)(11 - i)) { Order = i, FrameCount = 1 }));
    }

    private static CodeWriter CreateWriter()
    {
        var builder = CreateBuilder();
        var placer = new TowerCodePlacer(builder);
        placer.EnterStatementBlock();
        return new CodeWriter(placer, new TerminalConnector(builder.Connect));
    }

    private static PrefabBlockBuilder CreateBuilder()
    {
        Prefab prefab = new Prefab(RawGame.CurrentNumbStockPrefabs);
        var builder = new PrefabBlockBuilder(prefab);
        return builder;
    }

    private static AST Compile(CodeWriter writer)
    {
        writer.Flush();
        return Compile(writer.Placer.Builder);
    }

    private static AST Compile(BlockBuilder builder)
    {
        var prefabs = new PrefabList([(Prefab)builder.Build(int3.Zero)]);

        prefabs.AddImplicitConnections();

        return AST.Parse(prefabs, RawGame.CurrentNumbStockPrefabs);
    }
}

public static class ExecutionTestsDataSources
{
    public static IEnumerable<Func<ObjectWrapper>> InspectableLiterals()
    {
        yield return () => new(5.5f);
        yield return () => new(new float3(1f, 2f, 3f));
        yield return () => new(new Rotation(new float3(45f, 90f, 270f)));
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