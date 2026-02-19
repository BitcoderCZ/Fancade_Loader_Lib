using BitcoderCZ.Fancade.Editing.Scripting;
using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade.Editing.Tests;

public class FlatBuilderTests
{
    [Test]
    public async Task Constructor_InitializesEmpty()
    {
        var builder = new CodeGraph.FlatBuilder();

        await Assert.That(builder.NodeCount).IsEqualTo(0);
        await Assert.That(builder.CurrentExpressionDepth).IsEqualTo(0);
    }

    [Test]
    public async Task Place_AddsNode_IncrementsCount()
    {
        var builder = new CodeGraph.FlatBuilder();
        var def = StockBlocks.Variables.Set_Variable_Num;

        builder.Place(def);

        await Assert.That(builder.NodeCount).IsEqualTo(1);
        await Assert.That(builder.GetNode(0).Type).IsEqualTo(def);
    }

    [Test]
    public async Task PlaceEmptyNode_AddsNode_WithNullType()
    {
        var builder = new CodeGraph.FlatBuilder();

        builder.PlaceEmptyNode();

        await Assert.That(builder.NodeCount).IsEqualTo(1);
        await Assert.That(builder.GetNode(0).Type).IsNull();
    }

    [Test]
    public async Task ExpressionScope_NestsNodesCorrectly()
    {
        var builder = new CodeGraph.FlatBuilder();
        var def = StockBlocks.Variables.Set_Variable_Num;

        builder.Place(def); // Depth 0

        using (builder.ExpressionScope())
        {
            builder.Place(def); // Depth 1
            await Assert.That(builder.CurrentExpressionDepth).IsEqualTo(1);

            using (builder.ExpressionScope())
            {
                builder.Place(def); // Depth 2
            }
        }

        await Assert.That(builder.GetNode(0).ExpressionDepth).IsEqualTo(0);
        await Assert.That(builder.GetNode(1).ExpressionDepth).IsEqualTo(1);
        await Assert.That(builder.GetNode(2).ExpressionDepth).IsEqualTo(2);
        await Assert.That(builder.CurrentExpressionDepth).IsEqualTo(0);
    }

    [Test]
    public async Task ExitExpressionScope_WhenDepthIsZero_ThrowsInvalidOperation()
    {
        var builder = new CodeGraph.FlatBuilder();

        var action = () => builder.ExitExpressionScope();

        await Assert.That(action).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task CreateRegion_ReturnsValidRegion()
    {
        var builder = new CodeGraph.FlatBuilder();
        var size = new int3(2, 2, 2);

        var region = builder.CreateRegion(size);

        await Assert.That(region.Blocks.Size).IsEqualTo(size);
    }

    [Test]
    public async Task SetSetting_UpdatesNodeSettings()
    {
        var builder = new CodeGraph.FlatBuilder();
        ref var node = ref builder.Place(StockBlocks.Variables.Set_Variable_Num);
        var setting = new PrefabSetting(0, SettingType.String, "Abc");

        builder.SetSetting(node.Handle, setting);

        await Assert.That(builder.GetNode(node.Handle).Settings.Count).IsEqualTo(1);
    }

    [Test]
    public async Task BuildAndClear_FinalizesGraph_AndResetsBuilder()
    {
        var builder = new CodeGraph.FlatBuilder();
        builder.Place(StockBlocks.Variables.Set_Variable_Num);

        var graph = builder.BuildAndClear(false);

        await Assert.That(graph.NodeCount).IsEqualTo(1);
        await Assert.That(builder.NodeCount).IsEqualTo(0);
        await Assert.That(builder.CurrentExpressionDepth).IsEqualTo(0);
    }

    [Test]
    public async Task WriteToAndClear_MergesNodesToDestination()
    {
        var source = new CodeGraph.FlatBuilder();
        var destination = new CodeGraph.FlatBuilder();

        var type1 = StockBlocks.Variables.Set_Variable_Num;
        var type2 = StockBlocks.Variables.Set_Variable_Vec;
        source.Place(type1);
        destination.Place(type2);

        source.WriteToAndClear(destination);

        await Assert.That(destination.NodeCount).IsEqualTo(2);
        await Assert.That(source.NodeCount).IsEqualTo(0);

        var mergedNode = destination.GetNode(1);
        await Assert.That(mergedNode.Handle.GraphId).IsEqualTo(destination._graphId);
        await Assert.That(mergedNode.Type!.Prefab.Id).IsEqualTo(type1.Prefab.Id);
    }

    [Test]
    public async Task WriteToAndClear_CopiesNodeSettings()
    {
        var source = new CodeGraph.FlatBuilder();
        var destination = new CodeGraph.FlatBuilder();

        var type1 = StockBlocks.Variables.Set_Variable_Num;
        var type2 = StockBlocks.Variables.Set_Variable_Vec;
        ref var sourceNode =ref source.Place(type1);
        var setting = new PrefabSetting(0, 32f);
        sourceNode.AddSetting(setting);
        destination.Place(type2);

        source.WriteToAndClear(destination);

        await Assert.That(destination.NodeCount).IsEqualTo(2);
        await Assert.That(source.NodeCount).IsEqualTo(0);

        var mergedNode = destination.GetNode(1);
        await Assert.That(mergedNode.Handle.GraphId).IsEqualTo(destination._graphId);
        await Assert.That(mergedNode.Type!.Prefab.Id).IsEqualTo(type1.Prefab.Id);
        await Assert.That(mergedNode.Settings).IsEquivalentTo([setting]);
    }

    [Test]
    public async Task WriteToAndClear_RemapsConnectionTerminalsCorrectly()
    {
        var destBuilder = new CodeGraph.FlatBuilder();
        var destDef = StockBlocks.Variables.Set_Variable_Num;
        destBuilder.Place(destDef);

        var srcBuilder = new CodeGraph.FlatBuilder();
        var srcDef = StockBlocks.Variables.Set_Variable_Vec;

        ref var srcNodeA = ref srcBuilder.Place(srcDef);
        ref var srcNodeB = ref srcBuilder.Place(srcDef);

        var termA = new Scripting.Node.Terminal(srcNodeA.Handle, srcDef["After"]);
        var termB = new Scripting.Node.Terminal(srcNodeB.Handle, destDef["Before"]);
        srcBuilder.Connect(termA, termB);

        var sourceGraphId = termA.Node!.Value._graphId;

        srcBuilder.WriteToAndClear(destBuilder);

        var finalGraph = destBuilder.BuildAndClear(false);

        await Assert.That(finalGraph.NodeCount).IsEqualTo(3);
        await Assert.That(finalGraph.Connections.Count).IsEqualTo(1);

        var connection = finalGraph.Connections[0];

        await Assert.That(connection.From.Node!.Value._graphId).IsEqualTo(finalGraph._id);
        await Assert.That(connection.To.Node!.Value._graphId).IsEqualTo(finalGraph._id);

        await Assert.That(connection.From.Node!.Value._index).IsEqualTo((ushort)1);
        await Assert.That(connection.To.Node!.Value._index).IsEqualTo((ushort)2);
    }

    [Test]
    public async Task Connect_AddsConnectionBetweenTerminals()
    {
        var builder = new CodeGraph.FlatBuilder();
        var def = StockBlocks.Variables.Set_Variable_Num;
        ref var n1 = ref builder.Place(def);
        ref var n2 = ref builder.Place(def);

        var t1 = new Scripting.Node.Terminal(n1.Handle, def["After"]);
        var t2 = new Scripting.Node.Terminal(n2.Handle, def["Before"]);

        builder.Connect(t1, t2);
        var graph = builder.BuildAndClear(false);

        await Assert.That(graph.Connections.Count).IsEqualTo(1);
        await Assert.That(graph.Connections[0].From).IsEqualTo(t1);
        await Assert.That(graph.Connections[0].To).IsEqualTo(t2);
    }

    [Test]
    public async Task Clear_ResetsAllInternalState()
    {
        var builder = new CodeGraph.FlatBuilder();
        builder.Place(StockBlocks.Variables.Set_Variable_Num);
        builder.EnterExpressionScope();

        builder.Clear();

        await Assert.That(builder.NodeCount).IsEqualTo(0);
        await Assert.That(builder.CurrentExpressionDepth).IsEqualTo(0);
    }
}