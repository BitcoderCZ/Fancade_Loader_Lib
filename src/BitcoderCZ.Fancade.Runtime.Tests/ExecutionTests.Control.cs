using BitcoderCZ.Fancade.Runtime.Tests.Common;
using static BitcoderCZ.Fancade.Editing.Scripting.CodeWriter.Expressions;

namespace BitcoderCZ.Fancade.Runtime.Tests;

public partial class ExecutionTests
{
    [Test]
    public async Task Loop_Ascending()
    {
        var writer = CreateWriter();

        writer.Loop(None(), Literal(3f), (writer, index) =>
        {
            writer.Inspect(WrapTerminal(index));
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
            writer.Inspect(WrapTerminal(index));
        });

        var compiled = Compile(writer);

        await Assert.That(compiled).Inspects(
        [
            new(3f) { Order = 0, Frequency = InspectFrequency.EveryFrame },
            new(2f) { Order = 1, Frequency = InspectFrequency.EveryFrame },
            new(1f) { Order = 2, Frequency = InspectFrequency.EveryFrame },
        ]);
    }
}
