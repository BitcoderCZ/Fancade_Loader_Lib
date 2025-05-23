﻿using FancadeLoaderLib.Raw;
using FancadeLoaderLib.Tests.Common;
using MathUtils.Vectors;
using TUnit.Assertions.AssertConditions.Throws;

namespace FancadeLoaderLib.Tests;

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
