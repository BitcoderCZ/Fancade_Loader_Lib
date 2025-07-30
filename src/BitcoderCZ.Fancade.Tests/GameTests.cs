using BitcoderCZ.Fancade.Raw;
using BitcoderCZ.Fancade.Tests.Common;
using BitcoderCZ.Maths.Vectors;
using TUnit.Assertions.AssertConditions.Throws;

namespace BitcoderCZ.Fancade.Tests;

public class GameTests
{
    [Test]
    public async Task Constructor_NameAuthorDescriptionNotTooLong_DoesNotThrow()
    {
        string name = new string('a', 255);
        string author = new string('b', 255);
        string description = new string('c', 255);

        Game game = new Game(name, author, description, new());

        await Assert.That(game.Name).IsEqualTo(name);
        await Assert.That(game.Author).IsEqualTo(author);
        await Assert.That(game.Description).IsEqualTo(description);
    }

    [Test]
    public async Task Constructor_NameTooLong_Throws()
    {
        string name = new string('a', 256);

        await Assert.That(() => new Game(name)).Throws<ArgumentException>();
    }

    [Test]
    public async Task Constructor_NameEmpty_Throws()
    {
        string name = string.Empty;

        await Assert.That(() => new Game(name)).Throws<ArgumentException>();
    }

    [Test]
    public async Task Constructor_AuthorTooLong_Throws()
    {
        string author = new string('a', 256);

        await Assert.That(() => new Game("a", author, "a", new())).Throws<ArgumentException>();
    }

    [Test]
    public async Task Constructor_AuthorEmpty_Throws()
    {
        string author = string.Empty;

        await Assert.That(() => new Game("a", author, "a", new())).Throws<ArgumentException>();
    }

    [Test]
    public async Task Constructor_DescriptionTooLong_Throws()
    {
        string description = new string('a', 256);

        await Assert.That(() => new Game("a", "a", description, new())).Throws<ArgumentException>();
    }

    [Test]
    public async Task Constructor_DescriptionEmpty_DoesNotThrow()
    {
        string description = string.Empty;

        Game game = new Game("a", "a", description, new());

        await Assert.That(game.Description).IsEqualTo(description);
    }

    [Test]
    public async Task FromRaw_MixedSegments()
    {
        var raw = new RawGame("a");
        ushort idOff = raw.IdOffset;

        var rawBlocks = new Array3D<ushort>(int3.One * 3);
        // place A
        rawBlocks.Set(new int3(0, 0, 0), (ushort)(idOff + 4));
        rawBlocks.Set(new int3(1, 0, 0), (ushort)(idOff + 1));
        rawBlocks.Set(new int3(2, 0, 0), (ushort)(idOff + 6));
        // place B
        rawBlocks.Set(new int3(0, 0, 1), (ushort)(idOff + 2));
        rawBlocks.Set(new int3(1, 0, 1), (ushort)(idOff + 3));
        // place "C"
        rawBlocks.Set(new int3(0, 0, 2), (ushort)(idOff + 5));

        /*
         [index] group
         [0] not-in-group,
         [1] 0,
         [2] 1,
         [3] 1,
         [4] 0,
         [5] not-in-group,
         [6] 0,
         */
        raw.Prefabs.AddRange([
            new RawPrefab(false, false, false, false, false, 0, RawPrefab.DefaultName, 0, 0, (byte)FcColorUtils.DefaultBackgroundColor, 0, 0, new byte3(0, 0, 0), null, rawBlocks, null, null),
            new RawPrefab(true, false, false, false, false, 0, RawPrefab.DefaultName, 0, 0, (byte)FcColorUtils.DefaultBackgroundColor, 0, idOff, new byte3(1, 0, 0), null, null, null, null),
            new RawPrefab(true, false, false, false, false, 0, "B", 0, 0, (byte)FcColorUtils.DefaultBackgroundColor, 0, (ushort)(idOff + 3), byte3.Zero, null, null, null, null),
            new RawPrefab(true, false, false, false, false, 0, RawPrefab.DefaultName, 0, 0, (byte)FcColorUtils.DefaultBackgroundColor, 0, (ushort)(idOff + 3), new byte3(1, 0, 0), null, null, null, null),
            new RawPrefab(true, false, false, false, false, 0, RawPrefab.DefaultName, 0, 0, (byte)FcColorUtils.DefaultBackgroundColor, 0, idOff, byte3.Zero, null, null, null, null),
            new RawPrefab(false, false, false, false, false, 0, RawPrefab.DefaultName, 0, 0, (byte)FcColorUtils.DefaultBackgroundColor, 0, 0, new byte3(0, 0, 0), null, null, null, null),
            new RawPrefab(true, false, false, false, false, 0, "A", 0, 0, (byte)FcColorUtils.DefaultBackgroundColor, 0, idOff, new byte3(2, 0, 0), null, null, null, null),
        ]);

        var game = Game.FromRaw(raw, true);

        await Assert.That(game.Prefabs.SegmentCount).IsEqualTo(7);
        await Assert.That(game.Prefabs.PrefabCount).IsEqualTo(4);

        var blocks = game.Prefabs.GetPrefab(idOff).Blocks;

        await Assert.That(blocks.GetBlock(new int3(0, 0, 0))).IsEqualTo((ushort)(idOff + 1));
        await Assert.That(blocks.GetBlock(new int3(1, 0, 0))).IsEqualTo((ushort)(idOff + 2));
        await Assert.That(blocks.GetBlock(new int3(2, 0, 0))).IsEqualTo((ushort)(idOff + 3));
        await Assert.That(blocks.GetBlock(new int3(0, 0, 1))).IsEqualTo((ushort)(idOff + 4));
        await Assert.That(blocks.GetBlock(new int3(1, 0, 1))).IsEqualTo((ushort)(idOff + 5));
        await Assert.That(blocks.GetBlock(new int3(0, 0, 2))).IsEqualTo((ushort)(idOff + 6));

        var seg0 = game.Prefabs.GetSegment(blocks.GetBlock(new int3(0, 0, 0)));
        var seg1 = game.Prefabs.GetSegment(blocks.GetBlock(new int3(1, 0, 0)));
        var seg2 = game.Prefabs.GetSegment(blocks.GetBlock(new int3(2, 0, 0)));
        var seg3 = game.Prefabs.GetSegment(blocks.GetBlock(new int3(0, 0, 1)));
        var seg4 = game.Prefabs.GetSegment(blocks.GetBlock(new int3(1, 0, 1)));
        var seg5 = game.Prefabs.GetSegment(blocks.GetBlock(new int3(0, 0, 2)));

        await Assert.That(seg0.PrefabId).IsEqualTo((ushort)(idOff + 1));
        await Assert.That(seg0.PosInPrefab).IsEqualTo(new int3(0, 0, 0));
        await Assert.That(seg1.PrefabId).IsEqualTo((ushort)(idOff + 1));
        await Assert.That(seg1.PosInPrefab).IsEqualTo(new int3(1, 0, 0));
        await Assert.That(seg2.PrefabId).IsEqualTo((ushort)(idOff + 1));
        await Assert.That(seg2.PosInPrefab).IsEqualTo(new int3(2, 0, 0));
        await Assert.That(seg3.PrefabId).IsEqualTo((ushort)(idOff + 4));
        await Assert.That(seg3.PosInPrefab).IsEqualTo(new int3(0, 0, 0));
        await Assert.That(seg4.PrefabId).IsEqualTo((ushort)(idOff + 4));
        await Assert.That(seg4.PosInPrefab).IsEqualTo(new int3(1, 0, 0));
        await Assert.That(seg5.PrefabId).IsEqualTo((ushort)(idOff + 6));
        await Assert.That(seg5.PosInPrefab).IsEqualTo(new int3(0, 0, 0));
    }

    [Test]
    public async Task SaveLoad_PersistsAndRestoresData()
    {
        var game = new Game("A", "B", "C", new());

        BlockData blocks = new BlockData();
        blocks.SetBlock(new int3(1, 1, 1), 5);

        game.Prefabs.AddPrefab(new Prefab(RawGame.CurrentNumbStockPrefabs, "ABC", PrefabCollider.Box, PrefabType.Script, FcColor.Gray4, true, blocks, [new PrefabSetting(5, SettingType.Int, ushort3.One, 10)], [new Connection(ushort3.One, ushort3.One * 2, ushort3.Zero, ushort3.One)], [new PrefabSegment(RawGame.CurrentNumbStockPrefabs, int3.Zero)]));

        using (var ms = new MemoryStream())
        {
            using (var writer = new FcBinaryWriter(ms, true))
            {
                game.Save(writer);
            }

            ms.Position = 0;

            Game loadedGame;
            using (var reader = new FcBinaryReader(ms))
            {
                loadedGame = Game.Load(reader);
            }

            await Assert.That(loadedGame.Name).IsEqualTo(game.Name);
            await Assert.That(loadedGame.Author).IsEqualTo(game.Author);
            await Assert.That(loadedGame.Description).IsEqualTo(game.Description);

            using (Assert.Multiple())
            {
                await Assert.That(loadedGame.Prefabs.PrefabCount).IsEqualTo(game.Prefabs.PrefabCount);
                await Assert.That(loadedGame.Prefabs.SegmentCount).IsEqualTo(game.Prefabs.SegmentCount);
                await Assert.That(loadedGame.Prefabs.IdOffset).IsEqualTo(game.Prefabs.IdOffset);
            }

            await Assert.That(loadedGame.Prefabs.Prefabs).IsEquivalentTo(game.Prefabs.Prefabs, PrefabComparer.Instance);
            await Assert.That(loadedGame.Prefabs.Segments).IsEquivalentTo(game.Prefabs.Segments, PrefabSegmentComparer.Instance);
        }
    }

    [Test]
    public async Task SaveLoadCompressed_PersistsAndRestoresData()
    {
        var game = new Game("A", "B", "C", new());

        BlockData blocks = new BlockData();
        blocks.SetBlock(new int3(1, 1, 1), 5);

        game.Prefabs.AddPrefab(new Prefab(RawGame.CurrentNumbStockPrefabs, "ABC", PrefabCollider.Box, PrefabType.Script, FcColor.Gray4, true, blocks, [new PrefabSetting(5, SettingType.Int, ushort3.One, 10)], [new Connection(ushort3.One, ushort3.One * 2, ushort3.Zero, ushort3.One)], [new PrefabSegment(RawGame.CurrentNumbStockPrefabs, int3.Zero)]));

        using (var ms = new MemoryStream())
        {
            game.SaveCompressed(ms);

            ms.Position = 0;

            Game loadedGame = Game.LoadCompressed(ms);

            await Assert.That(loadedGame.Name).IsEqualTo(game.Name);
            await Assert.That(loadedGame.Author).IsEqualTo(game.Author);
            await Assert.That(loadedGame.Description).IsEqualTo(game.Description);

            using (Assert.Multiple())
            {
                await Assert.That(loadedGame.Prefabs.PrefabCount).IsEqualTo(game.Prefabs.PrefabCount);
                await Assert.That(loadedGame.Prefabs.SegmentCount).IsEqualTo(game.Prefabs.SegmentCount);
                await Assert.That(loadedGame.Prefabs.IdOffset).IsEqualTo(game.Prefabs.IdOffset);
            }

            await Assert.That(loadedGame.Prefabs.Prefabs).IsEquivalentTo(game.Prefabs.Prefabs, PrefabComparer.Instance);
            await Assert.That(loadedGame.Prefabs.Segments).IsEquivalentTo(game.Prefabs.Segments, PrefabSegmentComparer.Instance);
        }
    }
}
