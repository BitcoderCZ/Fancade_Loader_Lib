using BitcoderCZ.Fancade.Editing.Utils;
using BitcoderCZ.Fancade.Runtime.Tests.Common;
using static BitcoderCZ.Fancade.Editing.Scripting.CodeWriter.Expressions;
using static BitcoderCZ.Fancade.Runtime.Tests.Common.ExeUtils;

namespace BitcoderCZ.Fancade.Runtime.Tests;

public partial class ExecutionTests
{
    [Test]
    public async Task Loop_Ascending()
    {
        var writer = CreateWriter();

        writer.Loop(None(), Literal(3f), (writer, index) =>
        {
            writer.Inspect(index.Wrap());
        });

        var compiled = Compile(writer);

        await Assert.That(compiled).Inspects(
        [
            new(0f) { Order = 0, Frequency = InspectFrequency.EveryFrame },
            new(1f) { Order = 1, Frequency = InspectFrequency.EveryFrame },
            new(2f) { Order = 2, Frequency = InspectFrequency.EveryFrame },
        ]);
    }

    [Test]
    public async Task Loop_Descending()
    {
        var writer = CreateWriter();

        writer.Loop(Literal(3f), None(), (writer, index) =>
        {
            writer.Inspect(index.Wrap());
        });

        var compiled = Compile(writer);

        await Assert.That(compiled).Inspects(
        [
            new(3f) { Order = 0, Frequency = InspectFrequency.EveryFrame },
            new(2f) { Order = 1, Frequency = InspectFrequency.EveryFrame },
            new(1f) { Order = 2, Frequency = InspectFrequency.EveryFrame },
        ]);
    }

    [Test]
    public async Task If_TrueFalseExecutesBeforeAfter()
    {
        var writer = CreateWriter();

        writer.If(Truth(true),
        @true: writer =>
        {
            writer.Inspect(Number(1f));
        },
        @false: null);
        writer.Inspect(Number(2f));

        var compiled = Compile(writer);

        await Assert.That(compiled).Inspects(
        [
            new(1f) { Order = 0, Frequency = InspectFrequency.EveryFrame },
            new(2f) { Order = 1, Frequency = InspectFrequency.EveryFrame },
        ]);
    }

    [Test]
    public async Task PlaySensor()
    {
        var writer = CreateWriter();

        writer.PlaySensor(writer =>
        {
            writer.Inspect(Number(1f));
        });
        writer.Inspect(Number(2f));

        var compiled = Compile(writer);

        await Assert.That(compiled).Inspects(
        [
            new(1f) { Order = 0, Frequency = InspectFrequency.OnlyOnOneFrame, Count = 1, },
            new(2f) { Order = 1, Frequency = InspectFrequency.EveryFrame, FrameCount = 1, Count = 2 },
        ], runFor: 2);
    }
}
