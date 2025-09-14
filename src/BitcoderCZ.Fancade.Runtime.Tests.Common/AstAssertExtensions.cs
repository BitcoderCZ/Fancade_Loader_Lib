using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace BitcoderCZ.Fancade.Runtime.Tests.Common;

public static class AstAssertExtensions
{
    public static InvokableValueAssertionBuilder<FcAST> Inspects(this IValueSource<FcAST> valueSource, IEnumerable<InspectAssertExpected> asserts, int runFor = 2, (ushort, PrefabList)? physics = null, [CallerArgumentExpression(nameof(asserts))] string doNotPopulateThisValue1 = "")
        => valueSource
            .RegisterAssertion(new InspectsValueAssertCondition([.. asserts], runFor, TimeSpan.FromSeconds(3), true, physics), [doNotPopulateThisValue1]);
}
