using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade.Editing.Tests;

public class PrefabTerminalInfoTests
{
    [Test]
    [Arguments(1, 0, 0)]
    [Arguments(1, 1, 0)]
    [Arguments(1, 0, 1)]
    [Arguments(7, 0, 0)]
    [Arguments(7, 7, 6)]
    public async Task ConnectionToSelf_DoesNotCreateTerminal(int terminalPosX, int terminalPosY, int terminalPosZ)
    {
        var terminals = CreateTerminalsWithBlockConnection(new ushort3(terminalPosX, terminalPosY, terminalPosZ));

        await Assert.That(terminals.Terminals).IsEmpty();
    }

    [Test]
    [Arguments(0, 0, 0)]
    [Arguments(0, 7, 7)]
    [Arguments(3, 1, 7)]
    [Arguments(7, 7, 7)]
    public async Task BlockConnection_DoesCreateTerminal(int terminalPosX, int terminalPosY, int terminalPosZ)
    {
        var terminals = CreateTerminalsWithBlockConnection(new ushort3(terminalPosX, terminalPosY, terminalPosZ));

        await Assert.That(terminals.Terminals.Count).IsEqualTo(1);
    }

    private static PrefabTerminalInfo CreateTerminalsWithBlockConnection(ushort3 connectionPos)
    {
        var prefabs = new PrefabList();
        var prefab = Prefab.CreateBlock(0, "A");
        prefab[int3.Zero].Voxels.Fill(new Voxel(FcColor.Black, false));

        prefab.Blocks.SetPrefab(int3.Zero, StockBlocks.Variables.Set_Variable_Obj.Prefab);
        prefab.Connections.Add(new Connection(ushort3.One * Connection.IsFromToOutsideValue, ushort3.Zero, connectionPos, new ushort3(0, 1, 3)));

        return PrefabTerminalInfo.Create(prefab, prefabs);
    }
}
