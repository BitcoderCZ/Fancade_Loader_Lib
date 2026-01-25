using BitcoderCZ.Fancade.Runtime.Utils;
using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade.Runtime.Tests;

public class ScriptPositionComparerTests
{
    [Test]
    public async Task Compares_Correctly()
    {
        int3[] positions =
        [
            new int3(0, 0, 1),
            new int3(0, 1, 0),
            new int3(0, 0, 0),
            new int3(1, 0, 0),
        ];

        var sorted = positions.Reverse().Order(ScriptPositionComparer.Instance);

        await Assert.That(positions).IsEquivalentTo(sorted);
    }
}
