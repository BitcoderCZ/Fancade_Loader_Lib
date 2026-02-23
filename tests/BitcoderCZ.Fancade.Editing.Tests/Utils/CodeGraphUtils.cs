using BitcoderCZ.Fancade.Editing.Scripting;

namespace BitcoderCZ.Fancade.Editing.Tests.Utils;

internal static class CodeGraphUtils
{
    public static CodeGraph.Builder CreateBuilder(int nodeCount)
    {
        var builder = new CodeGraph.Builder(nodeCount);

        var blockType = StockBlocks.Objects.CreateObject;

        for (int i = 0; i < nodeCount; i++)
        {
            builder.Place(blockType);
        }

        return builder;
    }
}