using BitcoderCZ.Fancade.Raw;
using BitcoderCZ.Fancade.Tests.Common;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Frozen;
using System.Diagnostics;
using static BitcoderCZ.Fancade.Tests.Common.PrefabGenerator;

namespace BitcoderCZ.Fancade.Editing.Tests;

public class PrefabListUtilsTests
{
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task RemoveEmptySegmentsFromPrefab_DoesNotModifyNonEmptySegments(bool cache)
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab = CreatePrefab(1, 4, initVoxels: true);

        prefabList.AddPrefab(prefab);

        foreach (var segment in prefab.Values)
        {
            Debug.Assert(!segment.Voxels.IsEmpty);
            segment.Voxels[int3.Zero] = new Voxel(FcColor.Blue, false);
        }

        var prefabClone = prefab.Clone(true);

        prefabList.RemoveEmptySegmentsFromPrefab(prefab.Id, cache: cache ? new BlockInstancesCache(prefabList.Prefabs, 1) : null);

        await Assert.That<Prefab>(prefab).IsEqualTo(prefabClone, PrefabComparer.Instance);
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task RemoveEmptySegments_RemovesEmptyVoxels(bool cache)
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab = CreatePrefab(1, [new int3(0, 0, 0), new int3(1, 0, 0), new int3(0, 0, 1), new int3(1, 0, 1)], initVoxels: true);

        prefabList.AddPrefab(prefab);

        prefabList.RemoveEmptySegmentsFromPrefab(prefab.Id, cache: cache ? new BlockInstancesCache(prefabList.Prefabs, 1) : null);

        await Assert.That(prefab.Size).IsEqualTo(int3.One);
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task RemoveEmptySegments_KeepsNonEmptySegments(bool cache)
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab = CreatePrefab(1, [new int3(0, 0, 0), new int3(1, 0, 0), new int3(0, 0, 1), new int3(1, 0, 1), new int3(0, 0, 2), new int3(1, 0, 2)], initVoxels: true);

        var seg1 = prefab[new int3(0, 0, 2)];
        seg1.Voxels[int3.Zero] = new Voxel(FcColor.Blue, false);

        var seg1Clone = seg1.Clone();

        var seg2 = prefab[new int3(1, 0, 2)];
        seg2.Voxels[new int3(1, 0, 0)] = new Voxel(FcColor.Green, true);

        var seg2Clone = seg2.Clone();

        prefabList.AddPrefab(prefab);

        prefabList.RemoveEmptySegmentsFromPrefab(prefab.Id, cache: cache ? new BlockInstancesCache(prefabList.Prefabs, 1) : null);

        await Assert.That(prefab.Size).IsEqualTo(new int3(2, 1, 1));

        using (Assert.Multiple())
        {
            await Assert.That(seg1.PosInPrefab).IsEqualTo(new int3(0, 0, 0));
            await Assert.That(seg2.PosInPrefab).IsEqualTo(new int3(1, 0, 0));
        }

        using (Assert.Multiple())
        {
            await Assert.That(seg1.Voxels.Data.SequenceEqual(seg1Clone.Voxels.Data)).IsTrue();
            await Assert.That(seg2.Voxels.Data.SequenceEqual(seg2Clone.Voxels.Data)).IsTrue();
        }
    }

    [Test]
    [MethodDataSource(nameof(GetTerminalsThatConnect))]
    public async Task AddImplicitConnections_AddsConnections(TerminalInfo terminal1, TerminalInfo terminal2, int3 block2Offset)
    {
        var prefabList = new PrefabList()
        {
            IdOffset = RawGame.CurrentNumbStockPrefabs,
        };

        var prefab1 = CreatePrefab(0, (terminal1.Position / 8) + int3.One);
        var prefab2 = CreatePrefab(0, (terminal2.Position / 8) + int3.One);

        prefabList.AddPrefab(prefab1);
        prefabList.AddPrefab(prefab2);

        var blocks = prefab1.Blocks;
        blocks.SetPrefab(new int3(0, 0, 0), prefab1);

        var block2Pos = terminal1.Direction switch
        {
            TerminalDirection.PositiveX => new int3(prefab1.Size.X, 0, 0),
            TerminalDirection.PositiveZ => new int3(0, 0, prefab1.Size.Z),
            _ => throw new UnreachableException(),
        } + block2Offset;
        blocks.SetPrefab(block2Pos, prefab2);

        var terminals = new Dictionary<ushort, PrefabTerminalInfo>()
        {
            [prefab1.Id] = new PrefabTerminalInfo([terminal1]),
            [prefab2.Id] = new PrefabTerminalInfo([terminal2]),
        };

        prefabList.AddImplicitConnections(terminals.ToFrozenDictionary());

        using (Assert.Multiple())
        {
            await Assert.That(prefab1.Connections.Count).IsEqualTo(1);
            await Assert.That(prefab2.Connections.Count).IsEqualTo(0);
        }

        var connection = prefab1.Connections.First();

        using (Assert.Multiple())
        {
            await Assert.That(connection.FromVoxel).IsEqualTo(terminal1.IsInput ? terminal2.Position : terminal1.Position);
            await Assert.That(connection.ToVoxel).IsEqualTo(terminal1.IsInput ? terminal1.Position : terminal2.Position);
        }

        using (Assert.Multiple())
        {
            await Assert.That((int3)connection.From).IsEqualTo(terminal1.IsInput ? block2Pos : int3.Zero);
            await Assert.That((int3)connection.To).IsEqualTo(terminal1.IsInput ? int3.Zero : block2Pos);
        }
    }

    [Test]
    public async Task AddImplicitConnections_DoesNotAddConnections_IncorrectPos()
    {
        foreach (var item in GetTerminalsThatDoNotConnect())
        {
            var (terminal1, terminal2, block2Offset) = item;

            var prefabList = new PrefabList()
            {
                IdOffset = RawGame.CurrentNumbStockPrefabs,
            };

            var prefab1 = CreatePrefab(0, (terminal1.Position / 8) + int3.One);
            var prefab2 = CreatePrefab(0, (terminal2.Position / 8) + int3.One);

            prefabList.AddPrefab(prefab1);
            prefabList.AddPrefab(prefab2);

            var blocks = prefab1.Blocks;
            blocks.SetPrefab(new int3(0, 0, 0), prefab1);

            var block2Pos = terminal1.Direction switch
            {
                TerminalDirection.PositiveX => new int3(prefab1.Size.X, 0, 0),
                TerminalDirection.PositiveZ => new int3(0, 0, prefab1.Size.Z),
                _ => throw new UnreachableException(),
            } + block2Offset;
            blocks.SetPrefab(block2Pos, prefab2);

            var terminals = new Dictionary<ushort, PrefabTerminalInfo>()
            {
                [prefab1.Id] = new PrefabTerminalInfo([terminal1]),
                [prefab2.Id] = new PrefabTerminalInfo([terminal2]),
            };

            prefabList.AddImplicitConnections(terminals.ToFrozenDictionary());

            using (Assert.Multiple())
            {
                await Assert.That(prefab1.Connections.Count).IsEqualTo(0);
                await Assert.That(prefab2.Connections.Count).IsEqualTo(0);
            }
        }
    }

    public static IEnumerable<(TerminalInfo, TerminalInfo, int3)> GetTerminalsThatConnect()
        => GetTerminalsThatConnectInternal()
        .SelectMany(SelectBlockPositions);

    public static IEnumerable<(TerminalInfo, TerminalInfo, int3)> GetTerminalsThatDoNotConnect()
        => GetTerminalsThatDoNotConnectInternal()
        .SelectMany(SelectBlockPositions);

    private static IEnumerable<(TerminalInfo, TerminalInfo, int3)> SelectBlockPositions((TerminalInfo, TerminalInfo) tupple)
    {
        var (terminal1, terminal2) = tupple;

        yield return (terminal1, terminal2, int3.Zero);

        for (int i = 1; i < 4; i++)
        {
            switch (terminal2.Direction)
            {
                case TerminalDirection.PositiveX:
                case TerminalDirection.NegativeX:
                    if (terminal2.Position.Z - i * 8 >= 0)
                    {
                        yield return (terminal1, terminal2 with { Position = terminal2.Position - new byte3(0, 0, i * 8) }, new int3(0, 0, i));
                    }

                    break;
                case TerminalDirection.PositiveZ:
                case TerminalDirection.NegativeZ:
                    if (terminal2.Position.X - i * 8 >= 0)
                    {
                        yield return (terminal1, terminal2 with { Position = terminal2.Position - new byte3(i * 8, 0, 0) }, new int3(i, 0, 0));
                    }

                    break;
            }
        }
    }

    private static IEnumerable<(TerminalInfo, TerminalInfo)> GetTerminalsThatConnectInternal()
    {
        // X
        for (int z = 0; z <= 14; z++)
        {
            for (int y = 0; y <= 2; y++)
            {
                for (int x = 14; x <= 15; x++)
                {
                    var pos = new byte3(x, y, z);
                    foreach (var item in EnumerateTerminalCombinations(pos, pos - new byte3(14, 0, 0), true))
                    {
                        yield return item;
                    }
                }
            }
        }

        // Z
        for (int z = 14; z <= 15; z++)
        {
            for (int y = 0; y <= 2; y++)
            {
                for (int x = 0; x <= 14; x++)
                {
                    var pos = new byte3(x, y, z);
                    foreach (var item in EnumerateTerminalCombinations(pos, pos - new byte3(0, 0, 14), false))
                    {
                        yield return item;
                    }
                }
            }
        }
    }

    private static IEnumerable<(TerminalInfo, TerminalInfo)> GetTerminalsThatDoNotConnectInternal()
    {
        // X
        for (int z1 = 0; z1 <= 14; z1++)
        {
            for (int y1 = 0; y1 <= 2; y1++)
            {
                for (int x1 = 13; x1 <= 15; x1++)
                {
                    var pos1 = new byte3(x1, y1, z1);
                    for (int z2 = 0; z2 <= 14; z2++)
                    {
                        for (int y2 = 0; y2 <= 2; y2++)
                        {
                            for (int x2 = 13; x2 <= 15; x2++)
                            {
                                var pos2 = new byte3(x2, y2, z2);
                                if (new int3((pos1.X + 2) % 8, pos1.Y, pos1.Z) == pos2)
                                {
                                    continue;
                                }

                                foreach (var item in EnumerateTerminalCombinations(pos1, pos2, true))
                                {
                                    yield return item;
                                }
                            }
                        }
                    }
                }
            }
        }

        // Z
        for (int z1 = 13; z1 <= 15; z1++)
        {
            for (int y1 = 0; y1 <= 2; y1++)
            {
                for (int x1 = 0; x1 <= 14; x1++)
                {
                    var pos1 = new byte3(x1, y1, z1);
                    for (int z2 = 13; z2 <= 15; z2++)
                    {
                        for (int y2 = 0; y2 <= 2; y2++)
                        {
                            for (int x2 = 0; x2 <= 14; x2++)
                            {
                                var pos2 = new byte3(x2, y2, z2);
                                if (new int3(pos1.X, pos1.Y, (pos1.Z + 2) % 8) == pos2)
                                {
                                    continue;
                                }

                                foreach (var item in EnumerateTerminalCombinations(pos1, pos2, false))
                                {
                                    yield return item;
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    private static IEnumerable<(TerminalInfo, TerminalInfo)> EnumerateTerminalCombinations(byte3 pos1, byte3 pos2, bool xDir)
    {
        yield return Create(SignalType.Void, SignalType.Void);
        yield return Create(SignalType.Float, SignalType.Float);
        yield return Create(SignalType.FloatPtr, SignalType.Float);
        yield return Create(SignalType.FloatPtr, SignalType.FloatPtr);

        (TerminalInfo, TerminalInfo) Create(SignalType type1, SignalType type2)
        {
            return xDir
                ? (new TerminalInfo(pos1, type1, TerminalDirection.PositiveX, false),
                    new TerminalInfo(pos2, type2, TerminalDirection.NegativeX, true))
                : (new TerminalInfo(pos1, type2, TerminalDirection.PositiveZ, true),
                    new TerminalInfo(pos2, type1, TerminalDirection.NegativeZ, false));
        }
    }
}
