using BitcoderCZ.Fancade.Editing.Scripting;
using BitcoderCZ.Fancade.Editing.Scripting.Positioners;
using BitcoderCZ.Maths.Vectors;
using static BitcoderCZ.Fancade.Editing.Tests.Utils.CodeGraphUtils;

namespace BitcoderCZ.Fancade.Editing.Tests;

public class StructuredCodeGraphPositionerTests
{
    [Test]
    [MethodDataSource(nameof(LayoutFuncData))]
    public async Task Layout_CrossGraphNodeConnection_RemapsIndicesCorrectly(Func<ReadOnlySpan<CodeGraph>, PositionedCodeGraph> layoutFunc)
    {
        // --- Arrange ---
        // Graph A: 2 nodes (Global indices 0, 1)
        var graphABuilder = CreateBuilder(nodeCount: 2);

        var nodeA = graphABuilder.GetNode(1);

        // Graph B: 3 nodes (Global indices 2, 3, 4)
        var graphBBuilder = CreateBuilder(nodeCount: 3);

        var nodeB = graphBBuilder.GetNode(0);

        // Create a connection from Graph A (Node 1) to Graph B (Node 0)
        // Expected indices: From = 1, To = 2 (0 + Graph B offset)
        graphABuilder.Connect(new Node.Terminal(nodeA, "After"), new Node.Terminal(nodeB, "Before"));

        ReadOnlySpan<CodeGraph> graphs = [graphABuilder.BuildAndClear(), graphBBuilder.BuildAndClear()];

        // --- Act ---
        var result = layoutFunc(graphs);

        // --- Assert ---
        var connection = result.Connections[0];
        await Assert.That(connection.From.Node!.Value.Index).IsEqualTo(1); // Graph A offset (0) + 1
        await Assert.That(connection.To.Node!.Value.Index).IsEqualTo(2); // Graph B offset (2) + 0 
    }

    [Test]
    [MethodDataSource(nameof(LayoutFuncData))]
    public async Task Layout_ConnectionToMissingGraph_ThrowsArgumentException(Func<ReadOnlySpan<CodeGraph>, PositionedCodeGraph> layoutFunc)
    {
        // --- Arrange ---
        var graphABuilder = CreateBuilder(nodeCount: 1);

        var nodeA = graphABuilder.GetNode(0);

        // Connection points to Graph ID 999 which isn't in the input span
        graphABuilder.Connect(new Node.Terminal(nodeA, "After"), new Node.Terminal(new NodeHandle(999, 0), new TerminalDef(default, default, default, default)));

        // --- Act & Assert ---
        await Assert.That(() => layoutFunc([graphABuilder.BuildAndClear()])).Throws<ArgumentException>();
    }

    [Test]
    [MethodDataSource(nameof(LayoutFuncData))]
    public async Task Layout_CrossGraphRegionConnection_RemapsRegionOffsets(Func<ReadOnlySpan<CodeGraph>, PositionedCodeGraph> layoutFunc)
    {
        // --- Arrange ---
        var graphABuilder = CreateBuilder(nodeCount: 1);
        var regionA = graphABuilder.CreateRegion(int3.One); // Region Index 0

        var graphBBuilder = CreateBuilder(nodeCount: 1);
        var regionB = graphBBuilder.CreateRegion(int3.One); // Region Index 0 (Global 1)
        var nodeB = graphBBuilder.GetNode(0);

        // Connection from Graph B (Node) to Graph A (Region)
        graphBBuilder.Connect(new Node.Terminal(nodeB, "Object"), Node.Terminal.ObjectRelative(regionA, int3.Zero, byte3.Zero));

        // --- Act ---
        var result = layoutFunc([graphABuilder.BuildAndClear(), graphBBuilder.BuildAndClear()]);

        // --- Assert ---
        var processedConn = result.Connections[0];
        // Graph B starts at node index 1.
        await Assert.That(processedConn.From.Node!.Value.Index).IsEqualTo(1);
        // Graph A region offset is 0.
        await Assert.That(processedConn.To.Region!.Value.Index).IsEqualTo(0);
        await Assert.That(processedConn.To.Type).IsEqualTo(Node.TerminalType.ObjectRelative);
    }

    [Test]
    [MethodDataSource(nameof(LayoutFuncData))]
    public async Task Layout_ReverseOrderConnections_Works(Func<ReadOnlySpan<CodeGraph>, PositionedCodeGraph> layoutFunc)
    {
        var graphABuilder = CreateBuilder(nodeCount: 5);

        var nodeA = graphABuilder.GetNode(0);

        var graphBBuilder = CreateBuilder(nodeCount: 5);

        var nodeB = graphBBuilder.GetNode(0);

        // Connection from B (last graph) back to A (first graph)
        graphBBuilder.Connect(new Node.Terminal(nodeB, "After"), new Node.Terminal(nodeA, "Before"));

        var result = layoutFunc([graphABuilder.BuildAndClear(), graphBBuilder.BuildAndClear()]);

        var conn = result.Connections[0];
        await Assert.That(conn.From.Node!.Value.Index).IsEqualTo(5); // Graph B offset
        await Assert.That(conn.To.Node!.Value.Index).IsEqualTo(0); // Graph A offset
    }

    public static IEnumerable<Func<Func<ReadOnlySpan<CodeGraph>, PositionedCodeGraph>>> LayoutFuncData()
    {
        yield return () => graphs => TowerCodeGraphPositioner.Layout(graphs);
        yield return () => graphs => StructuredCodeGraphPositioner.Layout(graphs);
    }
}