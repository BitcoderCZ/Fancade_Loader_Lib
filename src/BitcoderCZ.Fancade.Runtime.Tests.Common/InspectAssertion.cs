using System.Text;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace BitcoderCZ.Fancade.Runtime.Tests.Common;

[AssertionExtension("Inspects")]
public sealed class InspectAssertion : Assertion<AstRunnerTester>
{
    private readonly InspectAssertExpected _expected;

    public InspectAssertion(AssertionContext<AstRunnerTester> context, InspectAssertExpected expected)
        : base(context)
    {
        _expected = expected;
        ((AstRunnerTester)typeof(EvaluationContext<AstRunnerTester>)
            .GetField("_value", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(context.Evaluation)!)
            .AddExpectedInspect(_expected);
    }

    protected override async Task<AssertionResult> CheckAsync(EvaluationMetadata<AstRunnerTester> metadata)
        => metadata.Value!.GetResult(_expected);

    protected override string GetExpectation()
    {
        StringBuilder builder = new StringBuilder();

        //if (AllowOnlyExpectedInspects)
        //{
        //    builder.Append("to inspect only: ");
        //}
        //else
        //{
        builder.Append("to inspect: ");
        //}

        builder.Append($"'{_expected.Value}' of type {_expected.Type}");

        if (_expected.Position is not null)
        {
            builder.Append($" at {_expected.Position}");
        }

        if (_expected.BoxArt is { } boxArt)
        {
            builder.Append(boxArt ? " when taking box art" : " when not taking box art");
        }

        if (_expected.Count is not null)
        {
            builder.Append($" {_expected.Count} {(_expected.Count == 1 ? "time" : "times")} in total");
        }

        if (_expected.FrameCount is not null)
        {
            builder.Append($" {_expected.FrameCount} {(_expected.Count == 1 ? "time" : "times")} per frame");
        }

        if (_expected.Frequency is not null)
        {
            builder.Append($" {_expected.Frequency}");
        }

        if (_expected.Order is not null)
        {
            builder.Append($" with order {_expected.Order}");
        }

        return builder.ToString();
    }
}