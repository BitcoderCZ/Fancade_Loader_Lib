using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace FancadeLoaderLib.Runtime.Tests.AssertUtils;

internal static class AstAssertExtensions
{
    public static InvokableValueAssertionBuilder<AST> Inspects(this IValueSource<AST> valueSource, IEnumerable<InspectAssertExpected> asserts, int runFor = 2, [CallerArgumentExpression(nameof(asserts))] string doNotPopulateThisValue1 = "")
        => valueSource
                .RegisterAssertion(new InspectsValueAssertCondition([.. asserts], runFor, TimeSpan.FromSeconds(300), true), [doNotPopulateThisValue1]);
}
