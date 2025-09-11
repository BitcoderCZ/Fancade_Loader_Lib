using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitcoderCZ.Fancade.Editing.Tests;

public class StockBlocksTests
{
    [Test]
    public async Task PrefabList_IsCorrect()
    {
        var list = StockBlocks.PrefabList;

        await Assert.That(list.SegmentCount).IsEqualTo(Raw.RawGame.CurrentNumbStockPrefabs);
    }
}
