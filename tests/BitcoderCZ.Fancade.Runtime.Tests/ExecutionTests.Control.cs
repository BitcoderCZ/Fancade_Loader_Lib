using BitcoderCZ.Fancade.Editing.Scripting;
using BitcoderCZ.Fancade.Editing.Utils;
using BitcoderCZ.Fancade.Runtime.Tests.Common;
using static BitcoderCZ.Fancade.Editing.Scripting.CodeWriter.Expressions;

namespace BitcoderCZ.Fancade.Runtime.Tests;

public partial class ExecutionTests
{
    [Test]
    public async Task Loop_Ascending()
    {
        var writer = new CodeWriter(new CodeGraph.Builder());

        writer.Loop(None(), Literal(3f), (writer, index) =>
        {
            writer.Inspect(index.Wrap());
        });

        var tester = AstRunnerTester.Create(writer);

        await Assert.That(tester)
            .Inspects(new(0f) { Order = 0, Frequency = InspectFrequency.EveryFrame, })
            .And.Inspects(new(1f) { Order = 1, Frequency = InspectFrequency.EveryFrame, })
            .And.Inspects(new(2f) { Order = 2, Frequency = InspectFrequency.EveryFrame, });
    }

    [Test]
    public async Task Loop_Descending()
    {
        var writer = new CodeWriter(new CodeGraph.Builder());

        writer.Loop(Literal(3f), None(), (writer, index) =>
        {
            writer.Inspect(index.Wrap());
        });

        var tester = AstRunnerTester.Create(writer);

        await Assert.That(tester)
            .Inspects(new(3f) { Order = 0, Frequency = InspectFrequency.EveryFrame, })
            .And.Inspects(new(2f) { Order = 1, Frequency = InspectFrequency.EveryFrame, })
            .And.Inspects(new(1f) { Order = 2, Frequency = InspectFrequency.EveryFrame, });
    }

    [Test]
    public async Task If_TrueFalseExecutesBeforeAfter()
    {
        var writer = new CodeWriter(new CodeGraph.Builder());

        writer.If(Truth(true),
        @true: writer =>
        {
            writer.Inspect(Number(1f));
        },
        @false: null);
        writer.Inspect(Number(2f));

        var tester = AstRunnerTester.Create(writer);

        await Assert.That(tester)
            .Inspects(new(1f) { Order = 0, Frequency = InspectFrequency.EveryFrame, })
            .And.Inspects(new(2f) { Order = 1, Frequency = InspectFrequency.EveryFrame, });
    }

    [Test]
    public async Task PlaySensor()
    {
        var writer = new CodeWriter(new CodeGraph.Builder());

        writer.PlaySensor(writer =>
        {
            writer.Inspect(Number(1f));
        });
        writer.Inspect(Number(2f));

        var tester = AstRunnerTester.Create(writer, options: new() { RunFor = 2, });

        await Assert.That(tester)
            .Inspects(new(1f) { Order = 0, Frequency = InspectFrequency.OnlyOnOneFrame, Count = 1, })
            .And.Inspects(new(2f) { Order = 1, Frequency = InspectFrequency.EveryFrame, FrameCount = 1, Count = 2 });
    }
}
